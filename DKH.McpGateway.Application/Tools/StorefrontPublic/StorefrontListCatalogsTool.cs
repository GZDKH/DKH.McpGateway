using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// MCP tool — public storefront namespace.
/// Lists the catalogs linked to the caller's storefront. Requires a Scope.Storefront
/// API key and returns ONLY this storefront's catalogs (per-tenant isolation, ADR-060).
/// </summary>
[McpServerToolType]
[StorefrontPublicTool]
public static class StorefrontListCatalogsTool
{
    [McpServerTool(Name = "storefront_list_catalogs"), Description(
        "List catalogs available in this storefront. Requires a storefront-scoped API key. " +
        "Returns only this storefront's catalogs.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CancellationToken cancellationToken = default)
    {
        if (apiKeyContext.Scope != ApiKeyScope.Storefront)
        {
            throw new UnauthorizedAccessException(
                "storefront_list_catalogs requires a storefront-scoped API key.");
        }

        var storefrontId = apiKeyContext.StorefrontId
            ?? throw new UnauthorizedAccessException(
                "This tool requires a storefront-scoped API key.");

        await gate.EnsureMcpEnabledAsync(storefrontId, cancellationToken);

        var response = await storefrontCatalogClient.GetCatalogsAsync(
            new GetCatalogsRequest { StorefrontId = new GuidValue(storefrontId.ToString()) },
            cancellationToken: cancellationToken);

        var result = new
        {
            catalogs = response.Catalogs
                .Where(static c => c.IsVisible)
                .OrderBy(static c => c.DisplayOrder)
                .Select(static c => new
                {
                    catalogId = c.CatalogId?.Value,
                    displayName = c.DisplayName,
                    isDefault = c.IsDefault,
                    displayOrder = c.DisplayOrder,
                }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
