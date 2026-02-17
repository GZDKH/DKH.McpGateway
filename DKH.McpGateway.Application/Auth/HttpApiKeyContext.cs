using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Reads validated API key context from HttpContext.Items (set by ApiKeyAuthMiddleware).
/// </summary>
internal sealed class HttpApiKeyContext(IHttpContextAccessor httpContextAccessor) : IApiKeyContext
{
    public string? ApiKeyId => httpContextAccessor.HttpContext?.Items["ApiKeyId"] as string;

    public ApiKeyScope Scope => httpContextAccessor.HttpContext?.Items["ApiKeyScope"] is ApiKeyScope scope
        ? scope
        : ApiKeyScope.Unspecified;

    public IReadOnlyList<string> Permissions => httpContextAccessor.HttpContext?.Items["ApiKeyPermissions"] as IReadOnlyList<string> ?? [];

    public bool IsAuthenticated => ApiKeyId is not null;

    public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public void EnsurePermission(string permission)
    {
        if (!IsAuthenticated)
        {
            throw new UnauthorizedAccessException("API key not provided");
        }

        if (!HasPermission(permission))
        {
            throw new UnauthorizedAccessException(
                $"API key does not have required permission: {permission}");
        }
    }
}
