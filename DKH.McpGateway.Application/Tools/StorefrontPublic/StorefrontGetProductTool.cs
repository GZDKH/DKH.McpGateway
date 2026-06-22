using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using Grpc.Core;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// MCP tool — public storefront namespace.
/// Returns product details by SEO name, searching only this storefront's catalogs. Requires a
/// Scope.Storefront API key; a product outside the storefront's catalogs is unreachable (ADR-060).
/// </summary>
[McpServerToolType]
public static class StorefrontGetProductTool
{
    [McpServerTool(Name = "storefront_get_product"), Description(
        "Get product details by SEO name from this storefront's catalogs. Requires a storefront-scoped API key.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CatalogManagementService.CatalogManagementServiceClient catalogClient,
        ProductManagementService.ProductManagementServiceClient productClient,
        [Description("Product SEO name or slug")] string productSeoName,
        [Description("Language code (e.g. 'en', 'ru')")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var catalogSeoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            apiKeyContext, gate, storefrontCatalogClient, catalogClient, languageCode, cancellationToken);

        // Walk only the storefront's catalogs; a product outside them stays unreachable (isolation).
        foreach (var catalogSeoName in catalogSeoNames)
        {
            try
            {
                var product = await productClient.GetProductDetailAsync(
                    new GetProductDetailRequest
                    {
                        CatalogSeoName = catalogSeoName,
                        ProductSeoName = productSeoName,
                        LanguageCode = languageCode,
                    },
                    cancellationToken: cancellationToken);

                var found = new
                {
                    catalogSeoName,
                    id = product.Id,
                    code = product.Code,
                    name = product.Name,
                    seoName = product.SeoName,
                    description = product.Description,
                    price = product.CallForPrice ? (double?)null : product.Price,
                    currency = product.CurrencyCode,
                    brand = product.Brand?.Name,
                    categories = product.Categories.Select(static c => new { name = c.CategoryName, seoName = c.CategorySeoName }),
                };

                return JsonSerializer.Serialize(found, McpJsonDefaults.Options);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                // Not in this catalog — try the next one of the storefront's catalogs.
            }
        }

        throw new RpcException(new Status(
            StatusCode.NotFound,
            $"Product '{productSeoName}' was not found in this storefront's catalogs."));
    }
}
