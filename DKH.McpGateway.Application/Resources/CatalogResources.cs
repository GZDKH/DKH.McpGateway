using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;
using DKH.McpGateway.Application.Tools.Products;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
[Authorize(Policy = MerchantResourceAuthorization.PolicyName)]
public static class CatalogResources
{
    [McpServerResource(Name = "catalog://catalogs", MimeType = "application/json")]
    [Description("List of all available product catalogs with product counts.")]
    public static async Task<string> GetCatalogsAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        CatalogManagementService.CatalogManagementServiceClient client,
        CancellationToken cancellationToken = default)
    {
        var workspaceMetadata = MerchantResourceAuthorization.RequireReadAccess(
            apiKeyContext, httpContextAccessor);

        var response = await client.GetCatalogsAsync(
            new GetStorefrontCatalogsRequest { LanguageCode = "ru" },
            headers: workspaceMetadata,
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            catalogs = response.Catalogs.Select(static c => new
            {
                name = c.Name,
                seoName = c.SeoName,
                productCount = c.ProductCount,
            }),
        }, McpJsonDefaults.Options);
    }

    [McpServerResource(Name = "catalog://categories", MimeType = "application/json")]
    [Description("Category tree for a catalog. Provide catalogSeoName to select a specific catalog.")]
    public static async Task<string> GetCategoriesAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        CategoryManagementService.CategoryManagementServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var workspaceMetadata = MerchantResourceAuthorization.RequireReadAccess(
            apiKeyContext, httpContextAccessor);

        var response = await client.GetCategoryTreeAsync(
            new GetCategoryTreeRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
            },
            headers: workspaceMetadata,
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            catalog = catalogSeoName,
            categories = response.RootCategories.Select(MapCategoryNode),
        }, McpJsonDefaults.Options);
    }

    [McpServerResource(Name = "catalog://products", MimeType = "application/json")]
    [Description("Get detailed product information by SEO name.")]
    public static async Task<string> GetProductAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        ProductManagementService.ProductManagementServiceClient client,
        CounterpartyCrudService.CounterpartyCrudServiceClient counterpartyClient,
        [Description("Product SEO name or slug")] string productSeoName,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var workspaceMetadata = MerchantResourceAuthorization.RequireReadAccess(
            apiKeyContext, httpContextAccessor);

        var product = await client.GetProductDetailAsync(
            new GetProductDetailRequest
            {
                CatalogSeoName = catalogSeoName,
                ProductSeoName = productSeoName,
                LanguageCode = languageCode,
            },
            headers: workspaceMetadata,
            cancellationToken: cancellationToken);

        // ADR-020 — Brand/Manufacturer names resolved from counterparty links, not
        // the legacy product.Brand / product.Manufacturer proto fields.
        var (brandName, manufacturerName) = await ProductCounterpartyNameResolver.ResolveAsync(
            product, counterpartyClient, languageCode, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            name = product.Name,
            seoName = product.SeoName,
            code = product.Code,
            description = product.Description,
            price = product.CallForPrice ? (double?)null : product.Price,
            currency = product.CurrencyCode,
            brand = brandName,
            manufacturer = manufacturerName,
            categories = product.Categories.Select(static c => new { name = c.CategoryName, seoName = c.CategorySeoName }),
        }, McpJsonDefaults.Options);
    }

    private static object MapCategoryNode(CategoryNode node) => new
    {
        name = node.Name,
        seoName = node.SeoName,
        productCount = node.ProductCount,
        children = node.Children.Select(MapCategoryNode),
    };
}
