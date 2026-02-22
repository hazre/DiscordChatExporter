using System;
using DiscordChatExporter.Core.Native.Contracts;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class AsyncContractSpecs
{
    [Fact]
    public void Should_define_expected_status_codes()
    {
        ((int)NativeStatusCode.Success).Should().Be(0);
        ((int)NativeStatusCode.InvalidArgument).Should().Be(1);
        ((int)NativeStatusCode.NotFound).Should().Be(2);
        ((int)NativeStatusCode.InvalidState).Should().Be(3);
        ((int)NativeStatusCode.InternalError).Should().Be(4);
    }

    [Fact]
    public void Should_define_discovery_job_kinds()
    {
        Enum.GetNames<NativeJobKind>()
            .Should()
            .Contain([nameof(NativeJobKind.Export), "DiscoveryGuilds", "DiscoveryChannels"]);
    }
}
