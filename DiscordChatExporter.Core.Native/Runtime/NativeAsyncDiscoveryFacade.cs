using System;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeAsyncDiscoveryFacade
{
    public static ulong StartGuilds(NativeDiscoveryRequest request)
    {
        return NativeAsyncJobFacade.StartJob(
            NativeJobKind.DiscoveryGuilds,
            job => ExecuteGuildsJobAsync(job, request)
        );
    }

    public static ulong StartChannels(NativeChannelDiscoveryRequest request)
    {
        return NativeAsyncJobFacade.StartJob(
            NativeJobKind.DiscoveryChannels,
            job => ExecuteChannelsJobAsync(job, request)
        );
    }

    private static async Task ExecuteGuildsJobAsync(
        NativeExportJob job,
        NativeDiscoveryRequest request
    )
    {
        job.MarkRunning();

        try
        {
            var guilds = await NativeDiscoveryService.GetGuildsAsync(
                request,
                job.CancellationTokenSource.Token
            );

            job.ReportProgress(1, null, null);
            job.CompleteWithResult(NativeResponseFactory.FromGuilds(guilds), guilds.Count);
        }
        catch (OperationCanceledException ex)
        {
            var error = NativeResponseFactory.ToErrorResponse(ex);
            job.CompleteWithError(
                NativeJobState.Canceled,
                error,
                NativeResponseFactory.FromError(error)
            );
        }
        catch (Exception ex)
        {
            var error = NativeResponseFactory.ToErrorResponse(ex);
            job.CompleteWithError(
                NativeJobState.Failed,
                error,
                NativeResponseFactory.FromError(error)
            );
        }
    }

    private static async Task ExecuteChannelsJobAsync(
        NativeExportJob job,
        NativeChannelDiscoveryRequest request
    )
    {
        job.MarkRunning();

        try
        {
            var channels = await NativeDiscoveryService.GetChannelsAsync(
                request,
                job.CancellationTokenSource.Token
            );

            job.ReportProgress(1, null, null);
            job.CompleteWithResult(NativeResponseFactory.FromChannels(channels), channels.Count);
        }
        catch (OperationCanceledException ex)
        {
            var error = NativeResponseFactory.ToErrorResponse(ex);
            job.CompleteWithError(
                NativeJobState.Canceled,
                error,
                NativeResponseFactory.FromError(error)
            );
        }
        catch (Exception ex)
        {
            var error = NativeResponseFactory.ToErrorResponse(ex);
            job.CompleteWithError(
                NativeJobState.Failed,
                error,
                NativeResponseFactory.FromError(error)
            );
        }
    }
}
