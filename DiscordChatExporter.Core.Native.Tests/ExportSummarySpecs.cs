using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class ExportSummarySpecs
{
    [Fact]
    public void Should_serialize_success_summary_shape()
    {
        var summary = new NativeExportSummary(ExportedChannelCount: 2, Warnings: [], Errors: []);

        var json = NativeResponseFactory.FromSummary(summary);

        json.Should().Contain("\"ok\":true");
        json.Should().Contain("\"exportedChannelCount\":2");
        json.Should().Contain("\"warningCount\":0");
        json.Should().Contain("\"errorCount\":0");
    }
}
