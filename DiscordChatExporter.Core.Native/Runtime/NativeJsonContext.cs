using System.Text.Json.Serialization;
using DiscordChatExporter.Core.Native.Contracts;

namespace DiscordChatExporter.Core.Native.Runtime;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(NativeExportRequest))]
[JsonSerializable(typeof(NativeGuildsRequest))]
[JsonSerializable(typeof(NativeChannelsRequest))]
[JsonSerializable(typeof(NativeSuccessResponse))]
[JsonSerializable(typeof(NativeErrorResponse))]
[JsonSerializable(typeof(NativeGuildsResponse))]
[JsonSerializable(typeof(NativeChannelsResponse))]
[JsonSerializable(typeof(NativeJobStateResponse))]
[JsonSerializable(typeof(NativeJobEvent))]
internal partial class NativeJsonContext : JsonSerializerContext;
