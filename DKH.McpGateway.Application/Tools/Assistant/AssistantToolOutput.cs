namespace DKH.McpGateway.Application.Tools.Assistant;

internal static class AssistantToolOutput
{
    internal static string Json(object value) => JsonSerializer.Serialize(value, McpJsonDefaults.Options);
}
