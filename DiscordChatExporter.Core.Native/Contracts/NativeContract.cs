using System.Collections.Generic;

namespace DiscordChatExporter.Core.Native.Contracts;

public enum NativeOperation
{
    ExportChannels,
    ExportGuild,
    ExportDirectMessages,
    ExportAll,
}

public enum NativeThreadInclusionMode
{
    None,
    Active,
    All,
}

public sealed class NativeExportRequest
{
    public string? Token { get; init; }

    public bool? RespectRateLimits { get; init; }

    public string? Operation { get; init; }

    public string? OutputPath { get; init; }

    public string? Format { get; init; }

    public string? After { get; init; }

    public string? Before { get; init; }

    public string? Partition { get; init; }

    public string? IncludeThreads { get; init; } = "none";

    public string? Filter { get; init; }

    public int? Parallel { get; init; }

    public bool? Markdown { get; init; }

    public bool? Media { get; init; }

    public bool? ReuseMedia { get; init; }

    public string? MediaDir { get; init; }

    public string? Locale { get; init; }

    public bool? Utc { get; init; }

    public IReadOnlyList<string>? ChannelIds { get; init; }

    public string? GuildId { get; init; }

    public bool? IncludeVc { get; init; }

    public bool? IncludeDm { get; init; }

    public bool? IncludeGuilds { get; init; }

    public string? DataPackage { get; init; }
}

public sealed record NativeExecutionRequest(
    string Token,
    bool RespectRateLimits,
    NativeOperation Operation,
    string OutputPath,
    DiscordChatExporter.Core.Exporting.ExportFormat Format,
    DiscordChatExporter.Core.Discord.Snowflake? After,
    DiscordChatExporter.Core.Discord.Snowflake? Before,
    DiscordChatExporter.Core.Exporting.Partitioning.PartitionLimit PartitionLimit,
    NativeThreadInclusionMode ThreadInclusionMode,
    DiscordChatExporter.Core.Exporting.Filtering.MessageFilter MessageFilter,
    int Parallel,
    bool Markdown,
    bool Media,
    bool ReuseMedia,
    string? MediaDir,
    string? Locale,
    bool Utc,
    IReadOnlyList<DiscordChatExporter.Core.Discord.Snowflake> ChannelIds,
    DiscordChatExporter.Core.Discord.Snowflake? GuildId,
    bool IncludeVc,
    bool IncludeDm,
    bool IncludeGuilds,
    string? DataPackage
);

public sealed record NativeExportIssue(string ChannelId, string ChannelName, string Message);

public sealed record NativeExportSummary(
    int ExportedChannelCount,
    IReadOnlyList<NativeExportIssue> Warnings,
    IReadOnlyList<NativeExportIssue> Errors
);
