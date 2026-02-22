using System;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeAsyncExportFacade
{
    public static ulong Start(NativeExecutionRequest request)
    {
        var handle = NativeExportJobRegistry.CreateHandle();
        var job = new NativeExportJob(handle, request);

        if (!NativeExportJobRegistry.TryAdd(job))
            throw new InvalidOperationException("Failed to create export job.");

        _ = Task.Run(() => ExecuteJobAsync(job));
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
        {
            return NativeResponseFactory.FromError(
                new NativeErrorResponse
                {
                    Code = "NotFound",
                    Message = $"Export handle '{handle}' was not found.",
                    IsFatal = false,
                }
            );
        }

        return NativeResponseFactory.FromJobState(job.CreateStateResponse());
    }

    public static string AwaitResultJson(ulong handle)
    {
        if (!NativeExportJobRegistry.TryGet(handle, out var job) || job is null)
        {
            return NativeResponseFactory.FromError(
                new NativeErrorResponse
                {
                    Code = "NotFound",
                    Message = $"Export handle '{handle}' was not found.",
                    IsFatal = false,
                }
            );
        }

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

    private static async Task ExecuteJobAsync(NativeExportJob job)
    {
        job.MarkRunning();

        try
        {
            var summary = await NativeExportService.ExportAsync(
                job.Request,
                (fraction, channelId, channelName) =>
                {
                    job.ReportProgress(fraction, channelId, channelName);
                },
                job.ReportWarning,
                job.ReportError,
                job.CancellationTokenSource.Token
            );

            var resultJson = NativeResponseFactory.FromSummary(summary);
            job.CompleteWithSuccess(summary, resultJson);
        }
        catch (OperationCanceledException ex)
        {
            var error = NativeResponseFactory.ToErrorResponse(ex);
            var resultJson = NativeResponseFactory.FromError(error);
            job.CompleteWithError(NativeJobState.Canceled, error, resultJson);
        }
        catch (Exception ex)
        {
            var error = NativeResponseFactory.ToErrorResponse(ex);
            var resultJson = NativeResponseFactory.FromError(error);
            job.CompleteWithError(NativeJobState.Failed, error, resultJson);
        }
    }
}
