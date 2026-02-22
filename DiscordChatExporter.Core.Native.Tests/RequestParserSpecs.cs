using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class RequestParserSpecs
{
    [Fact]
    public void Should_parse_minimal_request_with_defaults()
    {
        const string json = """
            {"token":"abc","operation":"exportDirectMessages","outputPath":"./out"}
            """;

        var request = NativeRequestParser.Parse(json);

        request.RespectRateLimits.Should().BeTrue();
        request.Parallel.Should().Be(1);
        request.Markdown.Should().BeTrue();
        request.Media.Should().BeFalse();
        request.ThreadInclusionMode.Should().Be(NativeThreadInclusionMode.None);
    }

    [Theory]
    [InlineData("""{"token":"","operation":"exportChannels","outputPath":"./out"}""")]
    [InlineData("""{"token":"x","operation":"unknown","outputPath":"./out"}""")]
    [InlineData(
        """{"token":"x","operation":"exportChannels","outputPath":"./out","channelIds":[]}"""
    )]
    [InlineData("""{"token":"x","operation":"exportGuild","outputPath":"./out"}""")]
    [InlineData(
        """{"token":"x","operation":"exportAll","outputPath":"./out","format":"bad-format"}"""
    )]
    public void Should_reject_invalid_requests(string json)
    {
        var action = () => NativeRequestParser.Parse(json);
        action.Should().Throw<NativeRequestValidationException>();
    }
}
