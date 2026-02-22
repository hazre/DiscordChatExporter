using DiscordChatExporter.Core.Exceptions;
using DiscordChatExporter.Core.Native.Runtime;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Core.Native.Tests;

public class ResponseFactorySpecs
{
    [Fact]
    public void Should_map_validation_exception_to_invalid_request_error()
    {
        var json = NativeResponseFactory.FromException(
            new NativeRequestValidationException("bad payload")
        );

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"code\":\"InvalidRequest\"");
        json.Should().Contain("\"isFatal\":false");
    }

    [Fact]
    public void Should_map_authentication_failure_to_auth_failed_error()
    {
        var json = NativeResponseFactory.FromException(
            new DiscordChatExporterException("Authentication token is invalid.", true)
        );

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"code\":\"AuthFailed\"");
        json.Should().Contain("\"isFatal\":true");
    }
}
