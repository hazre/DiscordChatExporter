using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
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

        return request.DirectMessages
            ? await GetDirectChannelsAsync(discord, cancellationToken)
            : await GetGuildChannelsAsync(discord, request, cancellationToken);
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
            channel.IsArchived
        );

    private static DiscordClient CreateDiscordClient(string token, bool respectRateLimits) =>
        new(
            token,
            respectRateLimits ? RateLimitPreference.RespectAll : RateLimitPreference.IgnoreAll
        );
}
