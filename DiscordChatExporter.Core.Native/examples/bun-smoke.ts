import { CString, dlopen, suffix } from "bun:ffi";
import { join } from "node:path";

function resolveRid(): string {
  const platform =
    process.platform === "win32"
      ? "win"
      : process.platform === "darwin"
        ? "osx"
        : "linux";

  const arch =
    process.arch === "x64"
      ? "x64"
      : process.arch === "arm64"
        ? "arm64"
        : process.arch;

  return `${platform}-${arch}`;
}

const rid = process.env.DCE_RID ?? resolveRid();
const libFileName = `DiscordChatExporter.Core.Native.${suffix}`;

const libPath = join(
  process.cwd(),
  "DiscordChatExporter.Core.Native",
  "bin",
  "Release",
  "net10.0",
  rid,
  "publish",
  libFileName,
);

const {
  symbols: {
    dce_export_json,
    dce_get_guilds_json,
    dce_get_channels_json,
    dce_free,
    dce_get_version,
  },
  close,
} = dlopen(libPath, {
  dce_export_json: { args: ["cstring"], returns: "ptr" },
  dce_get_guilds_json: { args: ["cstring"], returns: "ptr" },
  dce_get_channels_json: { args: ["cstring"], returns: "ptr" },
  dce_free: { args: ["ptr"], returns: "void" },
  dce_get_version: { args: [], returns: "ptr" },
});

const request = JSON.stringify({
  token: "invalid",
  operation: "exportDirectMessages",
  outputPath: "./out",
});

const versionPtr = dce_get_version();
try {
  console.log("version:", new CString(versionPtr).toString());
} finally {
  dce_free(versionPtr);
}

const requestBuffer = Buffer.from(`${request}\0`, "utf8");
const responsePtr = dce_export_json(requestBuffer);
try {
  const responseJson = new CString(responsePtr).toString();
  console.log("export:", responseJson);
} finally {
  dce_free(responsePtr);
}

const guildsRequestBuffer = Buffer.from(`${JSON.stringify({ token: "invalid" })}\0`, "utf8");
const guildsPtr = dce_get_guilds_json(guildsRequestBuffer);
try {
  const responseJson = new CString(guildsPtr).toString();
  console.log("guilds:", responseJson);
} finally {
  dce_free(guildsPtr);
}

const channelsRequestBuffer = Buffer.from(
  `${JSON.stringify({ token: "invalid", directMessages: true })}\0`,
  "utf8",
);
const channelsPtr = dce_get_channels_json(channelsRequestBuffer);
try {
  const responseJson = new CString(channelsPtr).toString();
  console.log("channels:", responseJson);
} finally {
  dce_free(channelsPtr);
  close();
}
