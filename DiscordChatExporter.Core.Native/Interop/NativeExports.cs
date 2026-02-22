using System;
using System.Reflection;
using System.Runtime.InteropServices;
using DiscordChatExporter.Core.Native.Runtime;

namespace DiscordChatExporter.Core.Native.Interop;

public static class NativeExports
{
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
            var summary = NativeExportService
                .ExportAsync(request, default)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return Utf8Heap.Allocate(NativeResponseFactory.FromSummary(summary));
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
