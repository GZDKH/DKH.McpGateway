namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// MCP gateway permission constants.
/// API keys must include these permissions to access corresponding tool categories.
/// </summary>
public static class McpPermissions
{
    /// <summary>
    /// Read access: search, get, list, stats, analytics tools.
    /// </summary>
    public const string Read = "mcp:read";

    /// <summary>
    /// Write access: create, update, delete, manage, import/export tools.
    /// </summary>
    public const string Write = "mcp:write";
}
