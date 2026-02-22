using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class JobRegistrySpecs
{
    [Fact]
    public void Should_create_unique_handles()
    {
        var h1 = NativeExportJobRegistry.CreateHandle();
        var h2 = NativeExportJobRegistry.CreateHandle();

        h2.Should().BeGreaterThan(h1);
    }
}
