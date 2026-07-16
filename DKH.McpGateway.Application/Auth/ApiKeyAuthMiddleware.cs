using DKH.ApiManagementService.Contracts.ApiManagement.Api.ApiKeyQuery.v1;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DKH.McpGateway.Application.Auth;

public sealed class ApiKeyAuthMiddleware(
    RequestDelegate next,
    ApiKeyQueryService.ApiKeyQueryServiceClient validationClient,
    IMemoryCache cache,
    ILogger<ApiKeyAuthMiddleware> logger)
{
    private const string ApiKeyHeader = "X-API-Key";
    private const string ProtectedResourceMetadataPath = "/.well-known/oauth-protected-resource";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task InvokeAsync(HttpContext context)
    {
        // MCP authentication normally serves protected-resource metadata before
        // this middleware. Keep the explicit bypass so a future pipeline reorder
        // cannot accidentally require an API key for OAuth discovery.
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics") ||
            context.Request.Path.StartsWithSegments(ProtectedResourceMetadataPath))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValues) ||
            string.IsNullOrWhiteSpace(apiKeyValues.ToString()))
        {
            logger.LogWarning("MCP request rejected: missing {Header} header", ApiKeyHeader);
            await RejectUnauthorizedAsync(context, "Missing API key");
            return;
        }

        var rawKey = apiKeyValues.ToString();
        var cacheKey = $"apikey:{rawKey}";

        if (!cache.TryGetValue(cacheKey, out ValidateApiKeyResponse? cached))
        {
            try
            {
                cached = await validationClient.ValidateApiKeyAsync(
                    new ValidateApiKeyRequest { RawKey = rawKey });

                cache.Set(cacheKey, cached, CacheTtl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate API key via ApiManagementService");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "API key validation service unavailable" });
                return;
            }
        }

        if (!cached!.IsValid)
        {
            logger.LogWarning("MCP request rejected: invalid API key. Reason: {Reason}", cached.ErrorReason);
            await RejectUnauthorizedAsync(context, cached.ErrorReason);
            return;
        }

        if (cached.Scope is not (ApiKeyScope.Mcp or ApiKeyScope.Storefront))
        {
            logger.LogWarning("MCP request rejected: invalid scope '{Scope}'. Expected 'mcp' or 'storefront'.", cached.Scope);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key scope for MCP access" });
            return;
        }

        context.Items["ApiKeyId"] = cached.ApiKeyId.Value;
        context.Items["ApiKeyScope"] = cached.Scope;
        context.Items["ApiKeyPermissions"] = cached.Permissions.ToList();

        // Storefront-scoped keys carry the bound storefront id (ApiManagement Contracts 1.6.0+),
        // which the public storefront_* tools use to isolate catalog data per tenant.
        if (cached.StorefrontId is not null && Guid.TryParse(cached.StorefrontId.Value, out var storefrontId))
        {
            context.Items["StorefrontId"] = storefrontId;
        }

        await next(context);
    }

    private static async Task RejectUnauthorizedAsync(HttpContext context, string? error)
    {
        await context.ChallengeAsync();
        if (!context.Response.HasStarted)
        {
            await context.Response.WriteAsJsonAsync(new { error });
        }
    }
}
