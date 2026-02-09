namespace DKH.McpGateway.Application.Tools.Common;

/// <summary>
/// Shared JSON serialization defaults for MCP tool responses.
/// </summary>
internal static class McpJsonDefaults
{
    internal static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
}
