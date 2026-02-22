using System;
using System.Reflection;
using System.Runtime.InteropServices;
using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;

namespace DiscordChatExporter.Core.Native.Interop;

public static class NativeExports
{
    [UnmanagedCallersOnly(EntryPoint = "dce_export_start_json")]
    public static int ExportStartJson(nint requestJsonPtr, nint outHandlePtr) =>
        (int)StartJobCore(
            requestJsonPtr,
            outHandlePtr,
            NativeRequestParser.Parse,
            NativeAsyncExportFacade.Start
        );

    [UnmanagedCallersOnly(EntryPoint = "dce_get_guilds_start_json")]
    public static int GetGuildsStartJson(nint requestJsonPtr, nint outHandlePtr) =>
        (int)StartJobCore(
            requestJsonPtr,
            outHandlePtr,
            NativeDiscoveryRequestParser.ParseGuilds,
            NativeAsyncDiscoveryFacade.StartGuilds
        );

    [UnmanagedCallersOnly(EntryPoint = "dce_get_channels_start_json")]
    public static int GetChannelsStartJson(nint requestJsonPtr, nint outHandlePtr) =>
        (int)StartJobCore(
            requestJsonPtr,
            outHandlePtr,
            NativeDiscoveryRequestParser.ParseChannels,
            NativeAsyncDiscoveryFacade.StartChannels
        );

    [UnmanagedCallersOnly(EntryPoint = "dce_job_set_callback")]
    public static int JobSetCallback(ulong handle, nint callbackPtr, nint userData) =>
        (int)SetJobCallbackCore(handle, callbackPtr, userData);

    [UnmanagedCallersOnly(EntryPoint = "dce_job_cancel")]
    public static int JobCancel(ulong handle) => (int)CancelJobCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_job_get_state_json")]
    public static nint JobGetStateJson(ulong handle) => GetJobStateJsonCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_job_await_result_json")]
    public static nint JobAwaitResultJson(ulong handle) => AwaitJobResultJsonCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_job_release")]
    public static int JobRelease(ulong handle) => (int)ReleaseJobCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_export_set_progress_callback")]
    public static int ExportSetProgressCallback(ulong handle, nint callbackPtr, nint userData) =>
        (int)SetJobCallbackCore(handle, callbackPtr, userData);

    [UnmanagedCallersOnly(EntryPoint = "dce_export_cancel")]
    public static int ExportCancel(ulong handle) => (int)CancelJobCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_export_get_state_json")]
    public static nint ExportGetStateJson(ulong handle) => GetJobStateJsonCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_export_await_result_json")]
    public static nint ExportAwaitResultJson(ulong handle) => AwaitJobResultJsonCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_export_release")]
    public static int ExportRelease(ulong handle) => (int)ReleaseJobCore(handle);

    [UnmanagedCallersOnly(EntryPoint = "dce_export_json")]
    public static nint ExportJson(nint requestJsonPtr)
    {
        try
        {
            var requestJson = Utf8Heap.Read(requestJsonPtr);
            var request = NativeRequestParser.Parse(requestJson);
            var handle = NativeAsyncExportFacade.Start(request);

            try
            {
                var resultJson = NativeAsyncExportFacade.AwaitResultJson(handle);
                return Utf8Heap.Allocate(resultJson);
            }
            finally
            {
                NativeAsyncExportFacade.Release(handle);
            }
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_free")]
    public static void Free(nint ptr) => Utf8Heap.Free(ptr);

    [UnmanagedCallersOnly(EntryPoint = "dce_get_version")]
    public static nint GetVersion()
    {
        var version =
            typeof(NativeExports)
                .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? typeof(NativeExports).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        return Utf8Heap.Allocate(version);
    }

    private static NativeStatusCode StartJobCore<TRequest>(
        nint requestJsonPtr,
        nint outHandlePtr,
        Func<string, TRequest> parseRequest,
        Func<TRequest, ulong> start
    )
    {
        try
        {
            if (requestJsonPtr == nint.Zero || outHandlePtr == nint.Zero)
                return NativeStatusCode.InvalidArgument;

            var requestJson = Utf8Heap.Read(requestJsonPtr);
            var request = parseRequest(requestJson);
            var handle = start(request);

            unsafe
            {
                *(ulong*)outHandlePtr = handle;
            }

            return NativeStatusCode.Success;
        }
        catch (NativeRequestValidationException)
        {
            return NativeStatusCode.InvalidArgument;
        }
        catch
        {
            return NativeStatusCode.InternalError;
        }
    }

    private static NativeStatusCode SetJobCallbackCore(
        ulong handle,
        nint callbackPtr,
        nint userData
    )
    {
        try
        {
            return NativeAsyncJobFacade.SetCallback(handle, callbackPtr, userData);
        }
        catch
        {
            return NativeStatusCode.InternalError;
        }
    }

    private static NativeStatusCode CancelJobCore(ulong handle)
    {
        try
        {
            return NativeAsyncJobFacade.Cancel(handle);
        }
        catch
        {
            return NativeStatusCode.InternalError;
        }
    }

    private static nint GetJobStateJsonCore(ulong handle)
    {
        try
        {
            var stateJson = NativeAsyncJobFacade.GetStateJson(handle);
            return Utf8Heap.Allocate(stateJson);
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

    private static nint AwaitJobResultJsonCore(ulong handle)
    {
        try
        {
            var resultJson = NativeAsyncJobFacade.AwaitResultJson(handle);
            return Utf8Heap.Allocate(resultJson);
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

    private static NativeStatusCode ReleaseJobCore(ulong handle)
    {
        try
        {
            return NativeAsyncJobFacade.Release(handle);
        }
        catch
        {
            return NativeStatusCode.InternalError;
        }
    }
}
