namespace DKH.McpGateway.Application.Tools.TelegramClient;

internal static class TelegramClientToolOutput
{
    internal static string Json(object value) => JsonSerializer.Serialize(value, McpJsonDefaults.Options);
}
