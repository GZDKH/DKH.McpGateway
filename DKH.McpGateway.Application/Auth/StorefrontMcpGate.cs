using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Enforces the per-storefront <c>McpEnabled</c> feature gate for storefront-scoped MCP access.
/// A storefront's catalog is only reachable through the public <c>storefront_*</c> tool namespace
/// when its <c>McpEnabled</c> feature is on (ADR-060, ApiPlatform Stage 1).
/// </summary>
public interface IStorefrontMcpGate
{
    /// <summary>
    /// Throws <see cref="RpcException"/> with <see cref="StatusCode.PermissionDenied"/> when the
    /// storefront has not enabled MCP access; returns normally when it has. Failures reading the
    /// storefront's features propagate (fail-closed) — the caller must not proceed when access
    /// cannot be confirmed.
    /// </summary>
    Task EnsureMcpEnabledAsync(Guid storefrontId, CancellationToken cancellationToken = default);
}

internal sealed class StorefrontMcpGate(
    StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient featuresClient,
    ILogger<StorefrontMcpGate> logger) : IStorefrontMcpGate
{
    public async Task EnsureMcpEnabledAsync(Guid storefrontId, CancellationToken cancellationToken = default)
    {
        // One features lookup per call (uncached). When the storefront_* tools put this on a
        // hot path, cache the result per storefront — see ApiKeyAuthMiddleware's IMemoryCache
        // 30s pattern.
        var response = await featuresClient.GetFeaturesAsync(
            new GetFeaturesRequest { StorefrontId = new GuidValue(storefrontId.ToString()) },
            cancellationToken: cancellationToken);

        if (response.Features?.McpEnabled == true)
        {
            return;
        }

        logger.LogWarning(
            "MCP access denied: storefront {StorefrontId} has not enabled MCP access", storefrontId);

        throw new RpcException(new Status(
            StatusCode.PermissionDenied,
            "This storefront has not enabled MCP access."));
    }
}
