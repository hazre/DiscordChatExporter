using System;
using System.Runtime.InteropServices;
using System.Text;
using DiscordChatExporter.Core.Native.Runtime;

namespace DiscordChatExporter.Core.Native.Interop;

public static class Utf8Heap
{
    public static nint Allocate(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value + '\0');
        var ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return ptr;
    }

    public static string Read(nint ptr)
    {
        if (ptr == nint.Zero)
            throw new NativeRequestValidationException("Input pointer is null.");

        return Marshal.PtrToStringUTF8(ptr)
            ?? throw new NativeRequestValidationException("Input pointer is invalid.");
    }

    public static void Free(nint ptr)
    {
        if (ptr != nint.Zero)
            Marshal.FreeHGlobal(ptr);
    }
}
