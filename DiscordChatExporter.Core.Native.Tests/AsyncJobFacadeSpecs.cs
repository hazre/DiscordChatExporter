using DiscordChatExporter.Core.Native.Contracts;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class AsyncJobFacadeSpecs
{
    [Fact]
    public void Should_return_not_found_for_unknown_job_handle_cancel()
    {
        var code = NativeAsyncJobFacade.Cancel(999_999);
        code.Should().Be(NativeStatusCode.NotFound);
    }

    [Fact]
    public void Should_return_not_found_for_unknown_job_handle_release()
    {
        var code = NativeAsyncJobFacade.Release(999_999);
        code.Should().Be(NativeStatusCode.NotFound);
    }

    [Fact]
    public void Should_return_not_found_json_for_unknown_job_handle_state()
    {
        var json = NativeAsyncJobFacade.GetStateJson(999_999);

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"code\":\"NotFound\"");
    }

    [Fact]
    public void Should_return_not_found_json_for_unknown_job_handle_result()
    {
        var json = NativeAsyncJobFacade.AwaitResultJson(999_999);

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"code\":\"NotFound\"");
    }
}
