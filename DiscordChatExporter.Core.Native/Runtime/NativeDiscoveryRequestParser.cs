using System;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeDiscoveryRequestParser
{
    public static NativeDiscoveryRequest ParseGuilds(string json)
    {
        var request =
            System.Text.Json.JsonSerializer.Deserialize(
                json,
                NativeJsonContext.Default.NativeGuildsRequest
            ) ?? throw new NativeRequestValidationException("Request payload is invalid.");

        var token = request.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new NativeRequestValidationException("'token' is required.");

        return new NativeDiscoveryRequest(token, request.RespectRateLimits ?? true);
    }

    public static NativeChannelDiscoveryRequest ParseChannels(string json)
    {
        var request =
            System.Text.Json.JsonSerializer.Deserialize(
                json,
                NativeJsonContext.Default.NativeChannelsRequest
            ) ?? throw new NativeRequestValidationException("Request payload is invalid.");

        var token = request.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new NativeRequestValidationException("'token' is required.");

        var directMessages = request.DirectMessages ?? false;
        var guildId = ParseNullableSnowflake(request.GuildId, "guildId");

        if (directMessages && guildId is not null)
        {
            throw new NativeRequestValidationException(
                "'guildId' cannot be used with 'directMessages=true'."
            );
        }

        if (!directMessages && guildId is null)
        {
            throw new NativeRequestValidationException(
                "Either 'guildId' or 'directMessages=true' is required."
            );
        }

        var includeVoiceChannels = request.IncludeVc ?? true;
        var includeThreads = ParseThreadInclusionMode(request.IncludeThreads);

        if (directMessages && includeThreads != NativeThreadInclusionMode.None)
        {
            throw new NativeRequestValidationException(
                "'includeThreads' is only valid for guild channels."
            );
        }

        return new NativeChannelDiscoveryRequest(
            token,
            request.RespectRateLimits ?? true,
            guildId,
            directMessages,
            includeVoiceChannels,
            includeThreads
        );
    }

    private static NativeThreadInclusionMode ParseThreadInclusionMode(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return NativeThreadInclusionMode.None;

        if (bool.TryParse(rawValue, out var boolValue))
        {
            return boolValue ? NativeThreadInclusionMode.Active : NativeThreadInclusionMode.None;
        }

        if (Enum.TryParse<NativeThreadInclusionMode>(rawValue, true, out var mode))
            return mode;

        throw new NativeRequestValidationException($"Invalid includeThreads value '{rawValue}'.");
    }

    private static Snowflake? ParseNullableSnowflake(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var snowflake = Snowflake.TryParse(value);
        if (snowflake is null)
            throw new NativeRequestValidationException(
                $"Invalid value for '{fieldName}': '{value}'."
            );

        return snowflake;
    }
}
