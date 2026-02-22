using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Discord.Dump;
using DiscordChatExporter.Core.Exceptions;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Utils.Extensions;
using Gress;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeExportService
{
    public static async ValueTask<NativeExportSummary> ExportAsync(
        NativeExecutionRequest request,
        Action<double, string?, string?>? onProgress = null,
        Action<NativeExportIssue>? onWarning = null,
        Action<NativeExportIssue>? onError = null,
        CancellationToken cancellationToken = default
    )
    {
        var discord = new DiscordClient(
            request.Token,
            request.RespectRateLimits
                ? RateLimitPreference.RespectAll
                : RateLimitPreference.IgnoreAll
        );

        var exporter = new ChannelExporter(discord);

        var channels = await ResolveChannelsAsync(discord, request, cancellationToken);
        channels = await IncludeThreadsAsync(discord, channels, request, cancellationToken);

        ValidateOutputPath(channels, request.OutputPath);

        var channelsById = new Dictionary<Snowflake, Channel>();
        foreach (var channel in channels)
            channelsById[channel.Id] = channel;
        var channelProgressById = channelsById.Keys.ToDictionary(channelId => channelId, _ => 0d);
        var progressSyncRoot = new object();

        void ReportChannelProgress(Snowflake channelId, double fraction)
        {
            if (channelProgressById.Count <= 0)
            {
                onProgress?.Invoke(1, null, null);
                return;
            }

            double overallFraction;
            lock (progressSyncRoot)
            {
                var clamped = Math.Clamp(fraction, 0, 1);
                channelProgressById[channelId] = Math.Max(channelProgressById[channelId], clamped);
                overallFraction = channelProgressById.Values.Sum() / channelProgressById.Count;
            }

            var channel = channelsById[channelId];
            onProgress?.Invoke(
                overallFraction,
                channel.Id.ToString(),
                channel.GetHierarchicalName()
            );
        }

        var errorsByChannel = new ConcurrentDictionary<Channel, string>();
        var warningsByChannel = new ConcurrentDictionary<Channel, string>();

        await Parallel.ForEachAsync(
            channels,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, request.Parallel),
                CancellationToken = cancellationToken,
            },
            async (channel, innerCancellationToken) =>
            {
                try
                {
                    var guild = await discord.GetGuildAsync(
                        channel.GuildId,
                        innerCancellationToken
                    );
                    var exportRequest = new ExportRequest(
                        guild,
                        channel,
                        request.OutputPath,
                        request.MediaDir,
                        request.Format,
                        request.After,
                        request.Before,
                        request.PartitionLimit,
                        request.MessageFilter,
                        request.Markdown,
                        request.Media,
                        request.ReuseMedia,
                        request.Locale,
                        request.Utc
                    );

                    var channelProgress = new Progress<Percentage>(p =>
                        ReportChannelProgress(channel.Id, p.Fraction)
                    );

                    await exporter.ExportChannelAsync(
                        exportRequest,
                        channelProgress,
                        innerCancellationToken
                    );

                    ReportChannelProgress(channel.Id, 1);
                }
                catch (ChannelEmptyException ex)
                {
                    warningsByChannel[channel] = ex.Message;
                    var warning = new NativeExportIssue(
                        channel.Id.ToString(),
                        channel.GetHierarchicalName(),
                        ex.Message
                    );
                    onWarning?.Invoke(warning);
                    ReportChannelProgress(channel.Id, 1);
                }
                catch (DiscordChatExporterException ex) when (!ex.IsFatal)
                {
                    errorsByChannel[channel] = ex.Message;
                    var error = new NativeExportIssue(
                        channel.Id.ToString(),
                        channel.GetHierarchicalName(),
                        ex.Message
                    );
                    onError?.Invoke(error);
                    ReportChannelProgress(channel.Id, 1);
                }
            }
        );

        onProgress?.Invoke(1, null, null);

        if (errorsByChannel.Count >= channels.Count)
            throw new DiscordChatExporterException("Export failed.");

        var warnings = warningsByChannel
            .OrderBy(x => x.Key.Id)
            .Select(x => new NativeExportIssue(
                x.Key.Id.ToString(),
                x.Key.GetHierarchicalName(),
                x.Value
            ))
            .ToArray();

        var errors = errorsByChannel
            .OrderBy(x => x.Key.Id)
            .Select(x => new NativeExportIssue(
                x.Key.Id.ToString(),
                x.Key.GetHierarchicalName(),
                x.Value
            ))
            .ToArray();

        return new NativeExportSummary(channels.Count - errors.Length, warnings, errors);
    }

    private static async ValueTask<List<Channel>> ResolveChannelsAsync(
        DiscordClient discord,
        NativeExecutionRequest request,
        CancellationToken cancellationToken
    )
    {
        return request.Operation switch
        {
            NativeOperation.ExportChannels => await ResolveExplicitChannelsAsync(
                discord,
                request.ChannelIds,
                cancellationToken
            ),
            NativeOperation.ExportGuild => await ResolveGuildChannelsAsync(
                discord,
                request.GuildId!.Value,
                request.IncludeVc,
                cancellationToken
            ),
            NativeOperation.ExportDirectMessages =>
            [
                .. await discord.GetGuildChannelsAsync(Guild.DirectMessages.Id, cancellationToken),
            ],
            NativeOperation.ExportAll => await ResolveAllChannelsAsync(
                discord,
                request,
                cancellationToken
            ),
            _ => throw new NativeRequestValidationException(
                $"Unsupported operation '{request.Operation}'."
            ),
        };
    }

    private static async ValueTask<List<Channel>> ResolveExplicitChannelsAsync(
        DiscordClient discord,
        IReadOnlyList<Snowflake> channelIds,
        CancellationToken cancellationToken
    )
    {
        var channels = new List<Channel>();
        var channelsByGuild = new Dictionary<Snowflake, IReadOnlyList<Channel>>();

        foreach (var channelId in channelIds)
        {
            var channel = await discord.GetChannelAsync(channelId, cancellationToken);

            if (!channel.IsCategory)
            {
                channels.Add(channel);
                continue;
            }

            if (!channelsByGuild.TryGetValue(channel.GuildId, out var guildChannels))
            {
                guildChannels = await discord.GetGuildChannelsAsync(
                    channel.GuildId,
                    cancellationToken
                );
                channelsByGuild[channel.GuildId] = guildChannels;
            }

            foreach (var guildChannel in guildChannels)
            {
                if (guildChannel.Parent?.Id == channel.Id)
                    channels.Add(guildChannel);
            }
        }

        return channels;
    }

    private static async ValueTask<List<Channel>> ResolveGuildChannelsAsync(
        DiscordClient discord,
        Snowflake guildId,
        bool includeVoiceChannels,
        CancellationToken cancellationToken
    )
    {
        var channels = new List<Channel>();

        await foreach (var channel in discord.GetGuildChannelsAsync(guildId, cancellationToken))
        {
            if (channel.IsCategory)
                continue;

            if (!includeVoiceChannels && channel.IsVoice)
                continue;

            channels.Add(channel);
        }

        return channels;
    }

    private static async ValueTask<List<Channel>> ResolveAllChannelsAsync(
        DiscordClient discord,
        NativeExecutionRequest request,
        CancellationToken cancellationToken
    )
    {
        var channels = new List<Channel>();

        if (string.IsNullOrWhiteSpace(request.DataPackage))
        {
            await foreach (var guild in discord.GetUserGuildsAsync(cancellationToken))
            {
                await foreach (
                    var channel in discord.GetGuildChannelsAsync(guild.Id, cancellationToken)
                )
                {
                    if (channel.IsCategory)
                        continue;

                    if (!request.IncludeVc && channel.IsVoice)
                        continue;

                    channels.Add(channel);
                }
            }
        }
        else
        {
            var dump = await DataDump.LoadAsync(request.DataPackage, cancellationToken);

            foreach (var dumpChannel in dump.Channels)
            {
                try
                {
                    var channel = await discord.GetChannelAsync(dumpChannel.Id, cancellationToken);
                    channels.Add(channel);
                }
                catch (DiscordChatExporterException)
                {
                    // Inaccessible channels are ignored, same as CLI behavior.
                }
            }
        }

        if (!request.IncludeDm)
            channels.RemoveAll(c => c.IsDirect);
        if (!request.IncludeGuilds)
            channels.RemoveAll(c => c.IsGuild);
        if (!request.IncludeVc)
            channels.RemoveAll(c => c.IsVoice);

        return channels;
    }

    private static async ValueTask<List<Channel>> IncludeThreadsAsync(
        DiscordClient discord,
        List<Channel> channels,
        NativeExecutionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.ThreadInclusionMode == NativeThreadInclusionMode.None)
            return channels;

        var unwrappedChannels = new List<Channel>(channels);

        var includeArchived = request.ThreadInclusionMode == NativeThreadInclusionMode.All;
        await foreach (
            var thread in discord.GetChannelThreadsAsync(
                channels,
                includeArchived,
                request.Before,
                request.After,
                cancellationToken
            )
        )
        {
            unwrappedChannels.Add(thread);
        }

        unwrappedChannels.RemoveAll(channel => channel.Kind == ChannelKind.GuildForum);
        return unwrappedChannels;
    }

    private static void ValidateOutputPath(IReadOnlyCollection<Channel> channels, string outputPath)
    {
        var isValidOutputPath =
            channels.Count <= 1
            || outputPath.Contains('%')
            || Directory.Exists(outputPath)
            || Path.EndsInDirectorySeparator(outputPath);

        if (!isValidOutputPath)
        {
            throw new NativeRequestValidationException(
                "Attempted to export multiple channels, but the output path is neither a directory nor a template. "
                    + "If the provided output path is meant to be treated as a directory, make sure it ends with a slash. "
                    + $"Provided output path: '{outputPath}'."
            );
        }
    }
}
