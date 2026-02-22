using System;
using System.Threading;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Interop;

namespace DiscordChatExporter.Core.Native.Runtime;

public sealed class NativeExportJob(ulong handle, NativeJobKind jobKind)
{
    private readonly object _syncRoot = new();
    private readonly object _emitSyncRoot = new();
    private readonly TaskCompletionSource<string> _completionSource = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private nint _callbackPtr;
    private nint _callbackUserData;

    public ulong Handle { get; } = handle;

    public NativeJobKind JobKind { get; } = jobKind;

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public NativeJobState State { get; private set; } = NativeJobState.Queued;

    public bool IsTerminal
    {
        get
        {
            var state = CurrentState;
            return state
                is NativeJobState.Succeeded
                    or NativeJobState.Failed
                    or NativeJobState.Canceled;
        }
    }

    public bool CancelRequested { get; private set; }

    public double Fraction { get; private set; }

    public int WarningCount { get; private set; }

    public int ErrorCount { get; private set; }

    public string? LastMessage { get; private set; }

    public Task<string> CompletionTask => _completionSource.Task;

    public NativeJobState CurrentState
    {
        get
        {
            lock (_syncRoot)
                return State;
        }
    }

    public void SetCallback(nint callbackPtr, nint callbackUserData)
    {
        lock (_syncRoot)
        {
            _callbackPtr = callbackPtr;
            _callbackUserData = callbackUserData;
        }
    }

    public void MarkRunning()
    {
        lock (_syncRoot)
            State = NativeJobState.Running;

        EmitEvent(
            new NativeJobEvent
            {
                Type = "status",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = "running",
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public void ReportProgress(double fraction, string? channelId, string? channelName)
    {
        double reportedFraction;

        lock (_syncRoot)
        {
            if (IsTerminal)
                return;

            Fraction = Math.Clamp(Math.Max(Fraction, fraction), 0, 1);
            reportedFraction = Fraction;
        }

        EmitEvent(
            new NativeJobEvent
            {
                Type = "progress",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = "running",
                Fraction = reportedFraction,
                ChannelId = channelId,
                ChannelName = channelName,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public void ReportWarning(NativeExportIssue issue)
    {
        lock (_syncRoot)
        {
            WarningCount++;
            LastMessage = issue.Message;
        }

        EmitEvent(
            new NativeJobEvent
            {
                Type = "warning",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = "running",
                Message = issue.Message,
                ChannelId = issue.ChannelId,
                ChannelName = issue.ChannelName,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public void ReportError(NativeExportIssue issue)
    {
        lock (_syncRoot)
        {
            ErrorCount++;
            LastMessage = issue.Message;
        }

        EmitEvent(
            new NativeJobEvent
            {
                Type = "error",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = "running",
                Message = issue.Message,
                ChannelId = issue.ChannelId,
                ChannelName = issue.ChannelName,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public void RequestCancellation()
    {
        lock (_syncRoot)
            CancelRequested = true;

        CancellationTokenSource.Cancel();
    }

    public void CompleteWithSuccess(NativeExportSummary summary, string resultJson)
    {
        lock (_syncRoot)
        {
            if (IsTerminal)
                return;

            State = NativeJobState.Succeeded;
            Fraction = 1;
            WarningCount = summary.Warnings.Count;
            ErrorCount = summary.Errors.Count;
        }

        _completionSource.TrySetResult(resultJson);

        EmitEvent(
            new NativeJobEvent
            {
                Type = "complete",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = "succeeded",
                Fraction = 1,
                Summary = new NativeSuccessResponse
                {
                    ExportedChannelCount = summary.ExportedChannelCount,
                    WarningCount = summary.Warnings.Count,
                    ErrorCount = summary.Errors.Count,
                    Warnings = summary.Warnings,
                    Errors = summary.Errors,
                },
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public void CompleteWithResult(string resultJson, int? itemCount = null)
    {
        lock (_syncRoot)
        {
            if (IsTerminal)
                return;

            State = NativeJobState.Succeeded;
            Fraction = 1;
        }

        _completionSource.TrySetResult(resultJson);

        EmitEvent(
            new NativeJobEvent
            {
                Type = "complete",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = "succeeded",
                Fraction = 1,
                ItemCount = itemCount,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public void CompleteWithError(
        NativeJobState terminalState,
        NativeErrorResponse error,
        string resultJson
    )
    {
        lock (_syncRoot)
        {
            if (IsTerminal)
                return;

            State = terminalState;
            LastMessage = error.Message;
        }

        _completionSource.TrySetResult(resultJson);

        EmitEvent(
            new NativeJobEvent
            {
                Type = "complete",
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = terminalState.ToString().ToLowerInvariant(),
                Error = error,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }
        );
    }

    public NativeJobStateResponse CreateStateResponse()
    {
        lock (_syncRoot)
        {
            return new NativeJobStateResponse
            {
                Handle = Handle,
                JobKind = JobKind.ToString(),
                State = State.ToString().ToLowerInvariant(),
                IsTerminal = IsTerminal,
                CancelRequested = CancelRequested,
                Fraction = Fraction,
                WarningCount = WarningCount,
                ErrorCount = ErrorCount,
                LastMessage = LastMessage,
            };
        }
    }

    public void Dispose()
    {
        CancellationTokenSource.Dispose();
    }

    private unsafe void EmitEvent(NativeJobEvent jobEvent)
    {
        lock (_emitSyncRoot)
        {
            nint callbackPtr;
            nint callbackUserData;

            lock (_syncRoot)
            {
                callbackPtr = _callbackPtr;
                callbackUserData = _callbackUserData;
            }

            if (callbackPtr == nint.Zero)
                return;

            var callback = (delegate* unmanaged[Cdecl]<ulong, nint, nint, void>)callbackPtr;

            var json = NativeResponseFactory.FromJobEvent(jobEvent);
            var jsonPtr = Utf8Heap.Allocate(json);

            callback(Handle, jsonPtr, callbackUserData);
        }
    }
}
