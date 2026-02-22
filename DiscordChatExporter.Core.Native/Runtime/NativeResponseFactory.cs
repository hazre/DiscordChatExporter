using System;
using System.Collections.Generic;
using DiscordChatExporter.Core.Exceptions;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeResponseFactory
{
    public static NativeErrorResponse CanceledError() =>
        new()
        {
            Code = "Canceled",
            Message = "Export canceled.",
            IsFatal = false,
        };

    public static string FromSummary(NativeExportSummary summary)
    {
        var response = new NativeSuccessResponse
        {
            ExportedChannelCount = summary.ExportedChannelCount,
            WarningCount = summary.Warnings.Count,
            ErrorCount = summary.Errors.Count,
            Warnings = summary.Warnings,
            Errors = summary.Errors,
        };

        return System.Text.Json.JsonSerializer.Serialize(
            response,
            NativeJsonContext.Default.NativeSuccessResponse
        );
    }

    public static string FromGuilds(IReadOnlyList<NativeGuildInfo> guilds)
    {
        var response = new NativeGuildsResponse { Guilds = guilds };
        return System.Text.Json.JsonSerializer.Serialize(
            response,
            NativeJsonContext.Default.NativeGuildsResponse
        );
    }

    public static string FromChannels(IReadOnlyList<NativeChannelInfo> channels)
    {
        var response = new NativeChannelsResponse { Channels = channels };
        return System.Text.Json.JsonSerializer.Serialize(
            response,
            NativeJsonContext.Default.NativeChannelsResponse
        );
    }

    public static string FromException(Exception ex)
    {
        var error = ToErrorResponse(ex);
        return FromError(error);
    }

    public static string FromError(NativeErrorResponse error) =>
        System.Text.Json.JsonSerializer.Serialize(
            error,
            NativeJsonContext.Default.NativeErrorResponse
        );

    public static string FromJobState(NativeJobStateResponse state) =>
        System.Text.Json.JsonSerializer.Serialize(
            state,
            NativeJsonContext.Default.NativeJobStateResponse
        );

    public static string FromJobEvent(NativeJobEvent jobEvent) =>
        System.Text.Json.JsonSerializer.Serialize(
            jobEvent,
            NativeJsonContext.Default.NativeJobEvent
        );

    public static NativeErrorResponse ToErrorResponse(Exception ex) =>
        ex switch
        {
            OperationCanceledException => CanceledError(),
            NativeRequestValidationException validation => new NativeErrorResponse
            {
                Code = "InvalidRequest",
                Message = validation.Message,
                IsFatal = false,
            },
            DiscordChatExporterException discord when IsAuthError(discord) =>
                new NativeErrorResponse
                {
                    Code = "AuthFailed",
                    Message = discord.Message,
                    IsFatal = true,
                },
            DiscordChatExporterException discord => new NativeErrorResponse
            {
                Code = "ExportFailed",
                Message = discord.Message,
                IsFatal = discord.IsFatal,
            },
            _ => new NativeErrorResponse
            {
                Code = "InternalError",
                Message = ex.Message,
                IsFatal = true,
            },
        };

    private static bool IsAuthError(DiscordChatExporterException ex) =>
        ex.IsFatal
        && ex.Message.Contains(
            "Authentication token is invalid.",
            StringComparison.OrdinalIgnoreCase
        );
}
