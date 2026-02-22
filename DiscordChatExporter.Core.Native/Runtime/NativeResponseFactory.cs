using System;
using System.Collections.Generic;
using DiscordChatExporter.Core.Exceptions;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeResponseFactory
{
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
        var error = ex switch
        {
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

        return System.Text.Json.JsonSerializer.Serialize(
            error,
            NativeJsonContext.Default.NativeErrorResponse
        );
    }

    private static bool IsAuthError(DiscordChatExporterException ex) =>
        ex.IsFatal
        && ex.Message.Contains(
            "Authentication token is invalid.",
            StringComparison.OrdinalIgnoreCase
        );
}
