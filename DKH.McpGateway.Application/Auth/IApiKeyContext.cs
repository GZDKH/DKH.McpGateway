using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Provides access to the validated API key context for the current request.
/// Injected by MCP tool methods that need permission checks.
/// </summary>
public interface IApiKeyContext
{
    string? ApiKeyId { get; }

    ApiKeyScope Scope { get; }

    IReadOnlyList<string> Permissions { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);

    void EnsurePermission(string permission);
}
