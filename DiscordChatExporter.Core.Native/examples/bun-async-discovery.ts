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
    dce_get_guilds_start_json,
    dce_get_channels_start_json,
    dce_job_set_callback,
    dce_job_get_state_json,
    dce_job_await_result_json,
    dce_job_release,
    dce_free,
  },
  close,
} = dlopen(libPath, {
  dce_get_guilds_start_json: { args: ["cstring", "ptr"], returns: "i32" },
  dce_get_channels_start_json: { args: ["cstring", "ptr"], returns: "i32" },
  dce_job_set_callback: { args: ["u64", "function", "ptr"], returns: "i32" },
  dce_job_get_state_json: { args: ["u64"], returns: "ptr" },
  dce_job_await_result_json: { args: ["u64"], returns: "ptr" },
  dce_job_release: { args: ["u64"], returns: "i32" },
  dce_free: { args: ["ptr"], returns: "void" },
});

const callback = new JSCallback(
  (handle, eventJsonPtr, _userData) => {
    try {
      const eventJson = new CString(eventJsonPtr).toString();
      console.log(`[event:${handle}] ${eventJson}`);
    } finally {
      dce_free(eventJsonPtr);
    }
  },
  {
    args: ["u64", "ptr", "ptr"],
    returns: "void",
    threadsafe: true,
  },
);

function startDiscovery(
  label: string,
  requestObj: object,
  startFn: (requestJson: Buffer, outHandle: BigUint64Array) => number,
): number {
  const request = Buffer.from(`${JSON.stringify(requestObj)}\0`, "utf8");
  const handleBuffer = new BigUint64Array(1);
  const startCode = startFn(request, handleBuffer);
  const handle = Number(handleBuffer[0]);

  if (startCode !== 0 || !handle) {
    throw new Error(`Failed to start ${label}: status=${startCode}, handle=${handle}`);
  }

  const callbackCode = dce_job_set_callback(handle, callback.ptr, 0);
  if (callbackCode !== 0) {
    throw new Error(`Failed to register callback for ${label}: status=${callbackCode}`);
  }

  const statePtr = dce_job_get_state_json(handle);
  try {
    console.log(`${label} state:`, new CString(statePtr).toString());
  } finally {
    dce_free(statePtr);
  }

  return handle;
}

function awaitAndRelease(label: string, handle: number) {
  const resultPtr = dce_job_await_result_json(handle);
  try {
    console.log(`${label} result:`, new CString(resultPtr).toString());
  } finally {
    dce_free(resultPtr);
  }

  const releaseCode = dce_job_release(handle);
  console.log(`${label} release status: ${releaseCode}`);
}

const guildsHandle = startDiscovery("guilds", { token: "invalid" }, dce_get_guilds_start_json);
awaitAndRelease("guilds", guildsHandle);

const channelsHandle = startDiscovery(
  "channels",
  { token: "invalid", directMessages: true },
  dce_get_channels_start_json,
);
awaitAndRelease("channels", channelsHandle);

await Bun.sleep(250);

callback.close();
close();
