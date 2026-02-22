using System;
using System.Collections.Generic;
using System.Globalization;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Partitioning;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

public static class NativeRequestParser
{
    public static NativeExecutionRequest Parse(string json)
    {
        var request =
            System.Text.Json.JsonSerializer.Deserialize(
                json,
                NativeJsonContext.Default.NativeExportRequest
            ) ?? throw new NativeRequestValidationException("Request payload is invalid.");

        var token = request.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new NativeRequestValidationException("'token' is required.");

        var outputPath = request.OutputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new NativeRequestValidationException("'outputPath' is required.");

        var parallel = request.Parallel ?? 1;
        if (parallel <= 0)
            throw new NativeRequestValidationException("'parallel' must be greater than zero.");

        var shouldDownloadAssets = request.Media ?? false;
        var shouldReuseAssets = request.ReuseMedia ?? false;
        if (shouldReuseAssets && !shouldDownloadAssets)
            throw new NativeRequestValidationException("'reuseMedia' requires 'media=true'.");

        if (!string.IsNullOrWhiteSpace(request.MediaDir) && !shouldDownloadAssets)
            throw new NativeRequestValidationException("'mediaDir' requires 'media=true'.");

        var operationRaw = request.Operation;
        if (
            string.IsNullOrWhiteSpace(operationRaw)
            || !Enum.TryParse<NativeOperation>(operationRaw, true, out var operation)
            || !Enum.IsDefined(operation)
        )
        {
            throw new NativeRequestValidationException($"Invalid operation '{operationRaw}'.");
        }

        var formatRaw = request.Format ?? nameof(ExportFormat.HtmlDark);
        if (!Enum.TryParse<ExportFormat>(formatRaw, true, out var format))
            throw new NativeRequestValidationException($"Invalid format '{formatRaw}'.");

        var after = ParseNullableSnowflake(request.After, "after");
        var before = ParseNullableSnowflake(request.Before, "before");

        PartitionLimit partitionLimit;
        try
        {
            partitionLimit = !string.IsNullOrWhiteSpace(request.Partition)
                ? PartitionLimit.Parse(request.Partition, CultureInfo.InvariantCulture)
                : PartitionLimit.Null;
        }
        catch (FormatException ex)
        {
            throw new NativeRequestValidationException(ex.Message);
        }

        MessageFilter messageFilter;
        try
        {
            messageFilter = !string.IsNullOrWhiteSpace(request.Filter)
                ? MessageFilter.Parse(request.Filter)
                : MessageFilter.Null;
        }
        catch (Exception ex)
        {
            throw new NativeRequestValidationException($"Invalid filter expression: {ex.Message}");
        }

        var includeThreads = ParseThreadInclusionMode(request.IncludeThreads);

        var channelIds = ParseChannelIds(request.ChannelIds);
        var guildId = ParseNullableSnowflake(request.GuildId, "guildId");

        ValidateOperationArguments(operation, channelIds, guildId);

        return new NativeExecutionRequest(
            token,
            request.RespectRateLimits ?? true,
            operation,
            outputPath,
            format,
            after,
            before,
            partitionLimit,
            includeThreads,
            messageFilter,
            parallel,
            request.Markdown ?? true,
            shouldDownloadAssets,
            shouldReuseAssets,
            request.MediaDir,
            request.Locale,
            request.Utc ?? false,
            channelIds,
            guildId,
            request.IncludeVc ?? true,
            request.IncludeDm ?? true,
            request.IncludeGuilds ?? true,
            request.DataPackage
        );
    }

    private static void ValidateOperationArguments(
        NativeOperation operation,
        IReadOnlyList<Snowflake> channelIds,
        Snowflake? guildId
    )
    {
        switch (operation)
        {
            case NativeOperation.ExportChannels:
            {
                if (channelIds.Count <= 0)
                {
                    throw new NativeRequestValidationException(
                        "'channelIds' is required for operation 'exportChannels'."
                    );
                }

                break;
            }

            case NativeOperation.ExportGuild:
            {
                if (guildId is null)
                {
                    throw new NativeRequestValidationException(
                        "'guildId' is required for operation 'exportGuild'."
                    );
                }

                break;
            }
        }
    }

    private static IReadOnlyList<Snowflake> ParseChannelIds(IReadOnlyList<string>? channelIds)
    {
        if (channelIds is null)
            return [];

        var parsed = new List<Snowflake>(channelIds.Count);
        foreach (var channelId in channelIds)
        {
            var snowflake = ParseNullableSnowflake(channelId, "channelIds[]");
            if (snowflake is null)
                throw new NativeRequestValidationException(
                    "'channelIds[]' contains invalid values."
                );

            parsed.Add(snowflake.Value);
        }

        return parsed;
    }

    private static Snowflake? ParseNullableSnowflake(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var snowflake = Snowflake.TryParse(value, CultureInfo.InvariantCulture);
        if (snowflake is null)
            throw new NativeRequestValidationException(
                $"Invalid value for '{fieldName}': '{value}'."
            );

        return snowflake;
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
}
