using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
public static class CatalogResources
{
    [McpServerResource(Name = "catalog://catalogs", MimeType = "application/json")]
    [Description("List of all available product catalogs with product counts.")]
    public static async Task<string> GetCatalogsAsync(
        CatalogManagementService.CatalogManagementServiceClient client,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCatalogsAsync(
            new GetStorefrontCatalogsRequest { LanguageCode = "ru" },
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
        CategoryManagementService.CategoryManagementServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCategoryTreeAsync(
            new GetCategoryTreeRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
            },
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
        ProductManagementService.ProductManagementServiceClient client,
        [Description("Product SEO name or slug")] string productSeoName,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var product = await client.GetProductDetailAsync(
            new GetProductDetailRequest
            {
                CatalogSeoName = catalogSeoName,
                ProductSeoName = productSeoName,
                LanguageCode = languageCode,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            name = product.Name,
            seoName = product.SeoName,
            code = product.Code,
            description = product.Description,
            price = product.CallForPrice ? (double?)null : product.Price,
            currency = product.CurrencyCode,
            brand = product.Brand?.Name,
            manufacturer = product.Manufacturer?.Name,
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
