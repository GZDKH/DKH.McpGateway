namespace DKH.McpGateway.Application.Tools.Engagement;

internal static class EngagementToolOutput
{
    internal static string Json(object value) => JsonSerializer.Serialize(value, McpJsonDefaults.Options);
}
