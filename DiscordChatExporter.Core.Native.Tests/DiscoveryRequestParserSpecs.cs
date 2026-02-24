using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class DiscoveryRequestParserSpecs
{
    [Fact]
    public void Should_parse_guilds_request_with_defaults()
    {
        const string json = """
            {"token":"abc"}
            """;

        var request = NativeDiscoveryRequestParser.ParseGuilds(json);
        request.Token.Should().Be("abc");
        request.RespectRateLimits.Should().BeTrue();
    }

    [Fact]
    public void Should_parse_direct_channel_request()
    {
        const string json = """
            {"token":"abc","directMessages":true}
            """;

        var request = NativeDiscoveryRequestParser.ParseChannels(json);

        request.DirectMessages.Should().BeTrue();
        request.GuildId.Should().BeNull();
        request.ThreadInclusionMode.Should().Be(NativeThreadInclusionMode.None);
        request.IncludeAccessibilityMetadata.Should().BeFalse();
        request.AccessibleOnly.Should().BeFalse();
    }

    [Fact]
    public void Should_parse_guild_channel_request()
    {
        const string json = """
            {"token":"abc","guildId":"123","includeVc":false,"includeThreads":"active"}
            """;

        var request = NativeDiscoveryRequestParser.ParseChannels(json);

        request.DirectMessages.Should().BeFalse();
        request.GuildId.Should().NotBeNull();
        request.IncludeVoiceChannels.Should().BeFalse();
        request.ThreadInclusionMode.Should().Be(NativeThreadInclusionMode.Active);
        request.IncludeAccessibilityMetadata.Should().BeFalse();
        request.AccessibleOnly.Should().BeFalse();
    }

    [Fact]
    public void Should_parse_channel_accessibility_options()
    {
        const string json = """
            {"token":"abc","guildId":"123","includeAccessibility":true,"accessibleOnly":true}
            """;

        var request = NativeDiscoveryRequestParser.ParseChannels(json);

        request.IncludeAccessibilityMetadata.Should().BeTrue();
        request.AccessibleOnly.Should().BeTrue();
    }

    [Theory]
    [InlineData("""{"token":"abc"}""")]
    [InlineData("""{"token":"abc","guildId":"123","directMessages":true}""")]
    [InlineData("""{"token":"abc","directMessages":true,"includeThreads":"all"}""")]
    [InlineData("""{"token":"abc","guildId":"123","includeThreads":"bad"}""")]
    public void Should_reject_invalid_channel_discovery_request(string json)
    {
        var action = () => NativeDiscoveryRequestParser.ParseChannels(json);
        action.Should().Throw<NativeRequestValidationException>();
    }
}
