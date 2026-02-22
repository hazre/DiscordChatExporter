using System.Collections.Concurrent;
using System.Threading;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeExportJobRegistry
{
    private static long _nextHandle;
    private static readonly ConcurrentDictionary<ulong, NativeExportJob> JobsByHandle = [];

    public static ulong CreateHandle() => (ulong)Interlocked.Increment(ref _nextHandle);

    public static bool TryAdd(NativeExportJob job) => JobsByHandle.TryAdd(job.Handle, job);

    public static bool TryGet(ulong handle, out NativeExportJob? job) =>
        JobsByHandle.TryGetValue(handle, out job);

    public static bool TryRemove(ulong handle, out NativeExportJob? job) =>
        JobsByHandle.TryRemove(handle, out job);
}
