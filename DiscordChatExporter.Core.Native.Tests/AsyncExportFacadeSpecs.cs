using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class AsyncExportFacadeSpecs
{
    [Fact]
    public void Should_return_not_found_for_unknown_handle_cancel()
    {
        var code = NativeAsyncExportFacade.Cancel(999_999);
        code.Should().Be(NativeStatusCode.NotFound);
    }

    [Fact]
    public void Should_return_not_found_for_unknown_handle_release()
    {
        var code = NativeAsyncExportFacade.Release(999_999);
        code.Should().Be(NativeStatusCode.NotFound);
    }

    [Fact]
    public void Should_return_not_found_json_for_unknown_handle_state()
    {
        var json = NativeAsyncExportFacade.GetStateJson(999_999);

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"code\":\"NotFound\"");
    }

    [Fact]
    public void Should_return_not_found_json_for_unknown_handle_result()
    {
        var json = NativeAsyncExportFacade.AwaitResultJson(999_999);

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"code\":\"NotFound\"");
    }
}
