using DiscordChatExporter.Core.Native.Interop;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class Utf8HeapSpecs
{
    [Fact]
    public void Should_roundtrip_utf8_heap_allocation_and_free()
    {
        var ptr = Utf8Heap.Allocate("hello");
        var value = Utf8Heap.Read(ptr);
        Utf8Heap.Free(ptr);

        value.Should().Be("hello");
    }
}
