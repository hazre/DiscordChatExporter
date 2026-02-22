using System;
using System.Reflection;
using System.Runtime.InteropServices;
using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;

namespace DiscordChatExporter.Core.Native.Interop;

public static class NativeExports
{
    [UnmanagedCallersOnly(EntryPoint = "dce_export_start_json")]
    public static int ExportStartJson(nint requestJsonPtr, nint outHandlePtr)
    {
        try
        {
            if (requestJsonPtr == nint.Zero || outHandlePtr == nint.Zero)
                return (int)NativeStatusCode.InvalidArgument;

            var requestJson = Utf8Heap.Read(requestJsonPtr);
            var request = NativeRequestParser.Parse(requestJson);

            var handle = NativeAsyncExportFacade.Start(request);
            unsafe
            {
                *(ulong*)outHandlePtr = handle;
            }

            return (int)NativeStatusCode.Success;
        }
        catch (NativeRequestValidationException)
        {
            return (int)NativeStatusCode.InvalidArgument;
        }
        catch
        {
            return (int)NativeStatusCode.InternalError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_export_set_progress_callback")]
    public static int ExportSetProgressCallback(ulong handle, nint callbackPtr, nint userData)
    {
        try
        {
            return (int)NativeAsyncExportFacade.SetCallback(handle, callbackPtr, userData);
        }
        catch
        {
            return (int)NativeStatusCode.InternalError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_export_cancel")]
    public static int ExportCancel(ulong handle)
    {
        try
        {
            return (int)NativeAsyncExportFacade.Cancel(handle);
        }
        catch
        {
            return (int)NativeStatusCode.InternalError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_export_get_state_json")]
    public static nint ExportGetStateJson(ulong handle)
    {
        try
        {
            var stateJson = NativeAsyncExportFacade.GetStateJson(handle);
            return Utf8Heap.Allocate(stateJson);
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_export_await_result_json")]
    public static nint ExportAwaitResultJson(ulong handle)
    {
        try
        {
            var resultJson = NativeAsyncExportFacade.AwaitResultJson(handle);
            return Utf8Heap.Allocate(resultJson);
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_export_release")]
    public static int ExportRelease(ulong handle)
    {
        try
        {
            return (int)NativeAsyncExportFacade.Release(handle);
        }
        catch
        {
            return (int)NativeStatusCode.InternalError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_get_guilds_json")]
    public static nint GetGuildsJson(nint requestJsonPtr)
    {
        try
        {
            var requestJson = Utf8Heap.Read(requestJsonPtr);
            var request = NativeDiscoveryRequestParser.ParseGuilds(requestJson);
            var guilds = NativeDiscoveryService
                .GetGuildsAsync(request, default)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return Utf8Heap.Allocate(NativeResponseFactory.FromGuilds(guilds));
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dce_get_channels_json")]
    public static nint GetChannelsJson(nint requestJsonPtr)
    {
        try
        {
            var requestJson = Utf8Heap.Read(requestJsonPtr);
            var request = NativeDiscoveryRequestParser.ParseChannels(requestJson);
            var channels = NativeDiscoveryService
                .GetChannelsAsync(request, default)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return Utf8Heap.Allocate(NativeResponseFactory.FromChannels(channels));
        }
        catch (Exception ex)
        {
            return Utf8Heap.Allocate(NativeResponseFactory.FromException(ex));
        }
    }

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
}
