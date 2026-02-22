using System.Collections.Generic;

namespace DiscordChatExporter.Core.Native.Contracts;

public sealed class NativeSuccessResponse
{
    public bool Ok { get; init; } = true;

    public required int ExportedChannelCount { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public required IReadOnlyList<NativeExportIssue> Warnings { get; init; }

    public required IReadOnlyList<NativeExportIssue> Errors { get; init; }
}

public sealed class NativeErrorResponse
{
    public bool Ok { get; init; } = false;

    public required string Code { get; init; }

    public required string Message { get; init; }

    public required bool IsFatal { get; init; }
}
