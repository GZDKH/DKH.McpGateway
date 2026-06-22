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

    /// <summary>
    /// Storefront the key is bound to (for <see cref="ApiKeyScope.Storefront"/> keys),
    /// or <c>null</c> for global (e.g. MCP-admin) keys. Drives per-tenant data isolation.
    /// </summary>
    Guid? StorefrontId { get; }

    IReadOnlyList<string> Permissions { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);

    void EnsurePermission(string permission);
}
