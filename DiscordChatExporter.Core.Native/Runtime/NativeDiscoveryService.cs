using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Exceptions;
using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Utils.Extensions;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeDiscoveryService
{
    public static async ValueTask<IReadOnlyList<NativeGuildInfo>> GetGuildsAsync(
        NativeDiscoveryRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var discord = CreateDiscordClient(request.Token, request.RespectRateLimits);

        var guilds = (await discord.GetUserGuildsAsync(cancellationToken))
            .OrderByDescending(g => g.Id == Guild.DirectMessages.Id)
            .ThenBy(g => g.Name)
            .Select(g => new NativeGuildInfo(g.Id.ToString(), g.Name, g.IsDirect))
            .ToArray();

        return guilds;
    }

    public static async ValueTask<IReadOnlyList<NativeChannelInfo>> GetChannelsAsync(
        NativeChannelDiscoveryRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var discord = CreateDiscordClient(request.Token, request.RespectRateLimits);

        var channels = request.DirectMessages
            ? await GetDirectChannelsAsync(discord, cancellationToken)
            : await GetGuildChannelsAsync(discord, request, cancellationToken);

        return request is { IncludeAccessibilityMetadata: false, AccessibleOnly: false }
            ? channels
            : await ApplyAccessibilityOptionsAsync(discord, channels, request, cancellationToken);
    }

    private static async ValueTask<IReadOnlyList<NativeChannelInfo>> GetGuildChannelsAsync(
        DiscordClient discord,
        NativeChannelDiscoveryRequest request,
        CancellationToken cancellationToken
    )
    {
        var guildId = request.GuildId!.Value;

        var channels = (await discord.GetGuildChannelsAsync(guildId, cancellationToken))
            .Where(c => !c.IsCategory)
            .Where(c => request.IncludeVoiceChannels || !c.IsVoice)
            .OrderBy(c => c.Parent?.Position)
            .ThenBy(c => c.Name)
            .ToArray();

        var threads =
            request.ThreadInclusionMode != NativeThreadInclusionMode.None
                ? (
                    await discord.GetGuildThreadsAsync(
                        guildId,
                        request.ThreadInclusionMode == NativeThreadInclusionMode.All,
                        null,
                        null,
                        cancellationToken
                    )
                )
                    .OrderBy(c => c.Name)
                    .ToArray()
                : [];

        var result = new List<NativeChannelInfo>(channels.Length + threads.Length);
        foreach (var channel in channels)
        {
            result.Add(ToChannelInfo(channel));

            foreach (var thread in threads.Where(t => t.Parent?.Id == channel.Id))
                result.Add(ToChannelInfo(thread));
        }

        return result;
    }

    private static async ValueTask<IReadOnlyList<NativeChannelInfo>> GetDirectChannelsAsync(
        DiscordClient discord,
        CancellationToken cancellationToken
    )
    {
        var channels = (
            await discord.GetGuildChannelsAsync(Guild.DirectMessages.Id, cancellationToken)
        )
            .OrderByDescending(c => c.LastMessageId)
            .ThenBy(c => c.Name)
            .Select(ToChannelInfo)
            .ToArray();

        return channels;
    }

    private static async ValueTask<IReadOnlyList<NativeChannelInfo>> ApplyAccessibilityOptionsAsync(
        DiscordClient discord,
        IReadOnlyList<NativeChannelInfo> channels,
        NativeChannelDiscoveryRequest request,
        CancellationToken cancellationToken
    )
    {
        if (channels.Count <= 0)
            return channels;

        var accessibilityByChannelId = new Dictionary<string, bool>(channels.Count);
        foreach (var channel in channels)
        {
            if (accessibilityByChannelId.ContainsKey(channel.Id))
                continue;

            var channelId = Snowflake.Parse(channel.Id);
            var isAccessible = await IsChannelAccessibleAsync(
                discord,
                channelId,
                cancellationToken
            );
            accessibilityByChannelId[channel.Id] = isAccessible;
        }

        return channels
            .Where(channel => !request.AccessibleOnly || accessibilityByChannelId[channel.Id])
            .Select(channel =>
                request.IncludeAccessibilityMetadata
                    ? channel with
                    {
                        IsAccessible = accessibilityByChannelId[channel.Id],
                    }
                    : channel
            )
            .ToArray();
    }

    private static async ValueTask<bool> IsChannelAccessibleAsync(
        DiscordClient discord,
        Snowflake channelId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Reuses export's read-access signal with one request and no history pagination.
            await using var enumerator = discord
                .GetMessagesAsync(channelId, null, channelId, null, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            _ = await enumerator.MoveNextAsync();

            return true;
        }
        catch (DiscordChatExporterException ex) when (!ex.IsFatal)
        {
            return false;
        }
    }

    private static NativeChannelInfo ToChannelInfo(Channel channel) =>
        new(
            channel.Id.ToString(),
            channel.GuildId.ToString(),
            channel.Parent?.Id.ToString(),
            channel.Name,
            channel.GetHierarchicalName(),
            channel.IsDirect,
            channel.IsVoice,
            channel.IsThread,
            channel.IsArchived,
            null
        );

    private static DiscordClient CreateDiscordClient(string token, bool respectRateLimits) =>
        new(
            token,
            respectRateLimits ? RateLimitPreference.RespectAll : RateLimitPreference.IgnoreAll
        );
}
