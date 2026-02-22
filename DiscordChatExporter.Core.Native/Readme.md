# DiscordChatExporter.Core.Native

NativeAOT shared library wrapper for `DiscordChatExporter.Core`, exposed via a C ABI.

## Build

Publish a shared library for a specific RID:

```powershell
dotnet publish DiscordChatExporter.Core.Native/DiscordChatExporter.Core.Native.csproj -c Release -r win-x64 --self-contained
dotnet publish DiscordChatExporter.Core.Native/DiscordChatExporter.Core.Native.csproj -c Release -r linux-x64 --self-contained
dotnet publish DiscordChatExporter.Core.Native/DiscordChatExporter.Core.Native.csproj -c Release -r osx-arm64 --self-contained
```

NativeAOT publish is typically done on the target platform/architecture (or equivalent cross-toolchain environment).

Output path:

`DiscordChatExporter.Core.Native/bin/Release/net10.0/<rid>/publish/`

## Exported Functions

- `dce_export_json(const char* request_json)` -> `const char* response_json`
- `dce_get_guilds_json(const char* request_json)` -> `const char* response_json`
- `dce_get_channels_json(const char* request_json)` -> `const char* response_json`
- `dce_free(void* ptr)` -> `void`
- `dce_get_version(void)` -> `const char* version`

All returned pointers must be released with `dce_free`.

## JSON Request Shape

Required:

- `token` (`string`)
- `operation` (`"exportChannels" | "exportGuild" | "exportDirectMessages" | "exportAll"`)
- `outputPath` (`string`)

Common optional fields:

- `respectRateLimits` (`bool`, default `true`)
- `format` (`string`, default `HtmlDark`)
- `after`, `before` (`string`, snowflake or date)
- `partition` (`string`, e.g. `"100"` or `"10mb"`)
- `includeThreads` (`"none" | "active" | "all"`, default `"none"`)
- `filter` (`string`)
- `parallel` (`int`, default `1`)
- `markdown` (`bool`, default `true`)
- `media` (`bool`, default `false`)
- `reuseMedia` (`bool`, default `false`)
- `mediaDir` (`string`)
- `locale` (`string`)
- `utc` (`bool`, default `false`)

Operation-specific fields:

- `exportChannels`: `channelIds` (`string[]`, required)
- `exportGuild`: `guildId` (`string`, required), `includeVc` (`bool`, default `true`)
- `exportAll`: `includeDm` (`bool`, default `true`), `includeGuilds` (`bool`, default `true`), `includeVc` (`bool`, default `true`), `dataPackage` (`string`)

## Discovery Request Shapes

`dce_get_guilds_json`:

- `token` (`string`, required)
- `respectRateLimits` (`bool`, default `true`)

`dce_get_channels_json`:

- `token` (`string`, required)
- `respectRateLimits` (`bool`, default `true`)
- Either:
  - `guildId` (`string`) with optional `includeVc` (`bool`, default `true`) and `includeThreads` (`"none" | "active" | "all"`, default `"none"`)
  - Or `directMessages` (`true`) to list DM channels.

## Bun FFI

See `DiscordChatExporter.Core.Native/examples/bun-smoke.ts` for a runnable example.
