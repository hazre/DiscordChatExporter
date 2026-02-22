import { CString, JSCallback, dlopen, suffix } from "bun:ffi";
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
    dce_export_start_json,
    dce_export_set_progress_callback,
    dce_export_get_state_json,
    dce_export_await_result_json,
    dce_export_release,
    dce_free,
  },
  close,
} = dlopen(libPath, {
  dce_export_start_json: { args: ["cstring", "ptr"], returns: "i32" },
  dce_export_set_progress_callback: { args: ["u64", "function", "ptr"], returns: "i32" },
  dce_export_get_state_json: { args: ["u64"], returns: "ptr" },
  dce_export_await_result_json: { args: ["u64"], returns: "ptr" },
  dce_export_release: { args: ["u64"], returns: "i32" },
  dce_free: { args: ["ptr"], returns: "void" },
});

const request = Buffer.from(
  `${JSON.stringify({
    token: "invalid",
    operation: "exportDirectMessages",
    outputPath: "./out",
  })}\0`,
  "utf8",
);

const handleBuffer = new BigUint64Array(1);
const startCode = dce_export_start_json(request, handleBuffer);
const handle = Number(handleBuffer[0]);

if (startCode !== 0 || !handle) {
  throw new Error(`Failed to start export job: status=${startCode}, handle=${handle}`);
}

const callback = new JSCallback(
  (cbHandle, eventPtr, _userData) => {
    try {
      const eventJson = new CString(eventPtr).toString();
      console.log(`[event:${cbHandle}] ${eventJson}`);
    } finally {
      dce_free(eventPtr);
    }
  },
  {
    args: ["u64", "ptr", "ptr"],
    returns: "void",
    threadsafe: true,
  },
);

const callbackCode = dce_export_set_progress_callback(handle, callback.ptr, 0);
if (callbackCode !== 0) {
  callback.close();
  throw new Error(`Failed to register callback: status=${callbackCode}`);
}

const statePtr = dce_export_get_state_json(handle);
try {
  console.log("state:", new CString(statePtr).toString());
} finally {
  dce_free(statePtr);
}

const resultPtr = dce_export_await_result_json(handle);
try {
  console.log("result:", new CString(resultPtr).toString());
} finally {
  dce_free(resultPtr);
}

const releaseCode = dce_export_release(handle);
console.log(`release status: ${releaseCode}`);

await Bun.sleep(250);

callback.close();
close();
