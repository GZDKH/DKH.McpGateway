using DKH.ApiManagementService.Contracts.Services.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DKH.McpGateway.Application.Auth;

public sealed class ApiKeyAuthMiddleware(
    RequestDelegate next,
    ApiKeyValidationService.ApiKeyValidationServiceClient validationClient,
    IMemoryCache cache,
    ILogger<ApiKeyAuthMiddleware> logger)
{
    private const string ApiKeyHeader = "X-API-Key";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValues) ||
            string.IsNullOrWhiteSpace(apiKeyValues.ToString()))
        {
            logger.LogWarning("MCP request rejected: missing {Header} header", ApiKeyHeader);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing API key" });
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = cached.ErrorReason });
            return;
        }

        context.Items["ApiKeyId"] = cached.ApiKeyId;
        context.Items["ApiKeyScope"] = cached.Scope;
        context.Items["ApiKeyPermissions"] = cached.Permissions.ToList();

        await next(context);
    }
}
