using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class DiscoveryResponseFactorySpecs
{
    [Fact]
    public void Should_serialize_guild_discovery_response_shape()
    {
        var json = NativeResponseFactory.FromGuilds([
            new NativeGuildInfo("1", "Guild A", false),
            new NativeGuildInfo("0", "Direct Messages", true),
        ]);

        json.Should().Contain("\"ok\":true");
        json.Should().Contain("\"guilds\":[");
        json.Should().Contain("\"isDirect\":true");
    }

    [Fact]
    public void Should_serialize_channel_discovery_response_shape()
    {
        var json = NativeResponseFactory.FromChannels([
            new NativeChannelInfo(
                "10",
                "1",
                null,
                "general",
                "general",
                false,
                false,
                false,
                false
            ),
        ]);

        json.Should().Contain("\"ok\":true");
        json.Should().Contain("\"channels\":[");
        json.Should().Contain("\"hierarchicalName\":\"general\"");
    }
}
