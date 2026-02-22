using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using DiscordChatExporter.Core.Native.Interop;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class NativeExportsSpecs
{
    [Fact]
    public void Should_expose_discovery_start_exports()
    {
        HasEntryPoint("dce_get_guilds_start_json").Should().BeTrue();
        HasEntryPoint("dce_get_channels_start_json").Should().BeTrue();
    }

    [Fact]
    public void Should_expose_shared_job_exports()
    {
        HasEntryPoint("dce_job_set_callback").Should().BeTrue();
        HasEntryPoint("dce_job_cancel").Should().BeTrue();
        HasEntryPoint("dce_job_get_state_json").Should().BeTrue();
        HasEntryPoint("dce_job_await_result_json").Should().BeTrue();
        HasEntryPoint("dce_job_release").Should().BeTrue();
    }

    [Fact]
    public void Should_not_expose_blocking_discovery_exports()
    {
        HasEntryPoint("dce_get_guilds_json").Should().BeFalse();
        HasEntryPoint("dce_get_channels_json").Should().BeFalse();
    }

    private static bool HasEntryPoint(string entryPoint) =>
        typeof(NativeExports)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(method =>
                method.GetCustomAttribute<UnmanagedCallersOnlyAttribute>()?.EntryPoint == entryPoint
            );
}
