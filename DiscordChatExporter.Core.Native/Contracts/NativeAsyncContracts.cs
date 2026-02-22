namespace DiscordChatExporter.Core.Native.Contracts;

public enum NativeStatusCode
{
    Success = 0,
    InvalidArgument = 1,
    NotFound = 2,
    InvalidState = 3,
    InternalError = 4,
}

public enum NativeJobState
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Canceled,
}

public sealed class NativeJobStateResponse
{
    public bool Ok { get; init; } = true;

    public required ulong Handle { get; init; }

    public required string State { get; init; }

    public required bool IsTerminal { get; init; }

    public required bool CancelRequested { get; init; }

    public required double Fraction { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public string? LastMessage { get; init; }
}

public sealed class NativeJobEvent
{
    public required string Type { get; init; }

    public required ulong Handle { get; init; }

    public string? State { get; init; }

    public double? Fraction { get; init; }

    public string? ChannelId { get; init; }

    public string? ChannelName { get; init; }

    public string? Message { get; init; }

    public NativeSuccessResponse? Summary { get; init; }

    public NativeErrorResponse? Error { get; init; }

    public required string Timestamp { get; init; }
}
