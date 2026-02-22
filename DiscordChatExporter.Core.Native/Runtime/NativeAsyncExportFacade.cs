using System;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeAsyncExportFacade
{
    public static ulong Start(NativeExecutionRequest request)
    {
        return NativeAsyncJobFacade.StartJob(
            NativeJobKind.Export,
            job => ExecuteJobAsync(job, request)
        );
    }

    public static NativeStatusCode SetCallback(ulong handle, nint callbackPtr, nint userData) =>
        NativeAsyncJobFacade.SetCallback(handle, callbackPtr, userData);

    public static NativeStatusCode Cancel(ulong handle) => NativeAsyncJobFacade.Cancel(handle);

    public static string GetStateJson(ulong handle) => NativeAsyncJobFacade.GetStateJson(handle);

    public static string AwaitResultJson(ulong handle) =>
        NativeAsyncJobFacade.AwaitResultJson(handle);

    public static NativeStatusCode Release(ulong handle) => NativeAsyncJobFacade.Release(handle);

    private static async Task ExecuteJobAsync(NativeExportJob job, NativeExecutionRequest request)
    {
        job.MarkRunning();

        try
        {
            var summary = await NativeExportService.ExportAsync(
                request,
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
