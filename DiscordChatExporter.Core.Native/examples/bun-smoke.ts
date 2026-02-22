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
    dce_get_guilds_start_json,
    dce_get_channels_start_json,
    dce_job_await_result_json,
    dce_job_release,
    dce_free,
    dce_get_version,
  },
  close,
} = dlopen(libPath, {
  dce_export_json: { args: ["cstring"], returns: "ptr" },
  dce_get_guilds_start_json: { args: ["cstring", "ptr"], returns: "i32" },
  dce_get_channels_start_json: { args: ["cstring", "ptr"], returns: "i32" },
  dce_job_await_result_json: { args: ["u64"], returns: "ptr" },
  dce_job_release: { args: ["u64"], returns: "i32" },
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

function runDiscovery(label: string, requestObj: object, startFn: (requestJson: Buffer, outHandle: BigUint64Array) => number) {
  const requestJson = Buffer.from(`${JSON.stringify(requestObj)}\0`, "utf8");
  const handleBuffer = new BigUint64Array(1);
  const startCode = startFn(requestJson, handleBuffer);
  const handle = Number(handleBuffer[0]);

  if (startCode !== 0 || !handle) {
    throw new Error(`Failed to start ${label} discovery job: status=${startCode}, handle=${handle}`);
  }

  const resultPtr = dce_job_await_result_json(handle);
  try {
    const responseJson = new CString(resultPtr).toString();
    console.log(`${label}:`, responseJson);
  } finally {
    dce_free(resultPtr);
  }

  const releaseCode = dce_job_release(handle);
  console.log(`${label} release status: ${releaseCode}`);
}

runDiscovery("guilds", { token: "invalid" }, dce_get_guilds_start_json);
runDiscovery("channels", { token: "invalid", directMessages: true }, dce_get_channels_start_json);

close();
