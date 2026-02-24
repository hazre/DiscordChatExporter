using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DiscordChatExporter.Core.Native.Contracts;

public sealed class NativeGuildsRequest
{
    public string? Token { get; init; }

    public bool? RespectRateLimits { get; init; }
}

public sealed class NativeChannelsRequest
{
    public string? Token { get; init; }

    public bool? RespectRateLimits { get; init; }

    public string? GuildId { get; init; }

    public bool? DirectMessages { get; init; }

    public bool? IncludeVc { get; init; }

    public string? IncludeThreads { get; init; }

    // Option shape is intentionally minimal and additive.
    // Future ABI revisions may collapse these into a single enum if needed.
    public bool? IncludeAccessibility { get; init; }

    public bool? AccessibleOnly { get; init; }
}

public sealed record NativeDiscoveryRequest(string Token, bool RespectRateLimits);

public sealed record NativeChannelDiscoveryRequest(
    string Token,
    bool RespectRateLimits,
    DiscordChatExporter.Core.Discord.Snowflake? GuildId,
    bool DirectMessages,
    bool IncludeVoiceChannels,
    NativeThreadInclusionMode ThreadInclusionMode,
    bool IncludeAccessibilityMetadata,
    bool AccessibleOnly
);

public sealed record NativeGuildInfo(string Id, string Name, bool IsDirect);

public sealed record NativeChannelInfo(
    string Id,
    string GuildId,
    string? ParentId,
    string Name,
    string HierarchicalName,
    bool IsDirect,
    bool IsVoice,
    bool IsThread,
    bool IsArchived,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsAccessible
);

public sealed class NativeGuildsResponse
{
    public bool Ok { get; init; } = true;

    public required IReadOnlyList<NativeGuildInfo> Guilds { get; init; }
}

public sealed class NativeChannelsResponse
{
    public bool Ok { get; init; } = true;

    public required IReadOnlyList<NativeChannelInfo> Channels { get; init; }
}
