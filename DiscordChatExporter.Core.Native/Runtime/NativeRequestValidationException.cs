using System;

namespace DiscordChatExporter.Core.Native.Runtime;

public sealed class NativeRequestValidationException(string message) : Exception(message);
