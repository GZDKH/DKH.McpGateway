using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// MCP tool — public storefront namespace.
/// Lists brands across this storefront's catalogs. Requires a Scope.Storefront API key;
/// brands come only from catalogs linked to the caller's storefront (ADR-060 isolation).
/// </summary>
[McpServerToolType]
public static class StorefrontListBrandsTool
{
    [McpServerTool(Name = "storefront_list_brands"), Description(
        "List brands available in this storefront's catalogs. Requires a storefront-scoped API key.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CatalogManagementService.CatalogManagementServiceClient catalogClient,
        BrandManagementService.BrandManagementServiceClient brandClient,
        [Description("Language code (e.g. 'en', 'ru')")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var catalogSeoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            apiKeyContext, gate, storefrontCatalogClient, catalogClient, languageCode, cancellationToken);

        // Dedupe by brand SEO name — a brand may appear in more than one of the storefront's catalogs.
        var brandsBySeoName = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var catalogSeoName in catalogSeoNames)
        {
            var response = await brandClient.GetBrandsAsync(
                new GetCatalogBrandsRequest { CatalogSeoName = catalogSeoName, LanguageCode = languageCode },
                cancellationToken: cancellationToken);

            foreach (var brand in response.Brands)
            {
                brandsBySeoName[brand.SeoName] = new
                {
                    name = brand.Name,
                    seoName = brand.SeoName,
                    description = brand.Description,
                    productCount = brand.ProductCount,
                };
            }
        }

        var result = new { brands = brandsBySeoName.Values };
        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
