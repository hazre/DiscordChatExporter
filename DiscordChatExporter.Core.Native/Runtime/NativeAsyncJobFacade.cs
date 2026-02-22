using System;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeAsyncJobFacade
{
    public static ulong StartJob(NativeJobKind jobKind, Func<NativeExportJob, Task> executeAsync)
    {
        var handle = NativeExportJobRegistry.CreateHandle();
        var job = new NativeExportJob(handle, jobKind);

        if (!NativeExportJobRegistry.TryAdd(job))
            throw new InvalidOperationException("Failed to create async job.");

        _ = Task.Run(async () =>
        {
            try
            {
                await executeAsync(job);
            }
            catch (Exception ex)
            {
                // Best effort fallback if specialized execution didn't handle completion.
                if (!job.IsTerminal)
                {
                    var error = NativeResponseFactory.ToErrorResponse(ex);
                    var resultJson = NativeResponseFactory.FromError(error);
                    var terminalState =
                        ex is OperationCanceledException
                            ? NativeJobState.Canceled
                            : NativeJobState.Failed;

                    job.CompleteWithError(terminalState, error, resultJson);
                }
            }
        });

        return handle;
    }

    public static NativeStatusCode SetCallback(ulong handle, nint callbackPtr, nint userData)
    {
        if (!NativeExportJobRegistry.TryGet(handle, out var job) || job is null)
            return NativeStatusCode.NotFound;

        if (job.IsTerminal)
            return NativeStatusCode.InvalidState;

        job.SetCallback(callbackPtr, userData);
        return NativeStatusCode.Success;
    }

    public static NativeStatusCode Cancel(ulong handle)
    {
        if (!NativeExportJobRegistry.TryGet(handle, out var job) || job is null)
            return NativeStatusCode.NotFound;

        if (job.IsTerminal)
        {
            return job.CurrentState == NativeJobState.Canceled
                ? NativeStatusCode.Success
                : NativeStatusCode.InvalidState;
        }

        job.RequestCancellation();
        return NativeStatusCode.Success;
    }

    public static string GetStateJson(ulong handle)
    {
        if (!NativeExportJobRegistry.TryGet(handle, out var job) || job is null)
            return FromNotFoundError(handle);

        return NativeResponseFactory.FromJobState(job.CreateStateResponse());
    }

    public static string AwaitResultJson(ulong handle)
    {
        if (!NativeExportJobRegistry.TryGet(handle, out var job) || job is null)
            return FromNotFoundError(handle);

        return job.CompletionTask.GetAwaiter().GetResult();
    }

    public static NativeStatusCode Release(ulong handle)
    {
        if (!NativeExportJobRegistry.TryGet(handle, out var job) || job is null)
            return NativeStatusCode.NotFound;

        if (!job.IsTerminal)
            return NativeStatusCode.InvalidState;

        if (!NativeExportJobRegistry.TryRemove(handle, out var removedJob) || removedJob is null)
            return NativeStatusCode.NotFound;

        removedJob.Dispose();
        return NativeStatusCode.Success;
    }

    private static string FromNotFoundError(ulong handle) =>
        NativeResponseFactory.FromError(
            new NativeErrorResponse
            {
                Code = "NotFound",
                Message = $"Job handle '{handle}' was not found.",
                IsFatal = false,
            }
        );
}
