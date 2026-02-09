using System.ComponentModel;
using System.Text.Json;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductSearchQuery.v1;
using ModelContextProtocol.Server;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tools for product catalog search and listing.
/// </summary>
[McpServerToolType]
public static class ProductTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Search products with filters and pagination.
    /// </summary>
    [McpServerTool(Name = "search_products"), Description("Search products in the catalog with optional filters for category, brand, and price range.")]
    public static async Task<string> SearchProductsAsync(
        ProductSearchQueryService.ProductSearchQueryServiceClient client,
        [Description("Search query text")] string query,
        [Description("Catalog SEO name (e.g. 'main-catalog')")] string catalogSeoName = "main-catalog",
        [Description("Language code (e.g. 'en', 'ru')")] string languageCode = "ru",
        [Description("Filter by brand SEO names (comma-separated)")] string? brandFilter = null,
        [Description("Minimum price filter")] double? priceMin = null,
        [Description("Maximum price filter")] double? priceMax = null,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var request = new SearchProductsRequest
        {
            SearchTerm = query,
            CatalogSeoName = catalogSeoName,
            LanguageCode = languageCode,
            Page = page,
            PageSize = pageSize,
        };

        if (priceMin.HasValue)
        {
            request.MinPrice = priceMin.Value;
        }

        if (priceMax.HasValue)
        {
            request.MaxPrice = priceMax.Value;
        }

        if (!string.IsNullOrEmpty(brandFilter))
        {
            request.BrandSeoNames.AddRange(
                brandFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        var response = await client.SearchProductsAsync(request, cancellationToken: cancellationToken);

        var result = new
        {
            totalCount = response.TotalCount,
            page,
            pageSize,
            products = response.Items.Select(static p => new
            {
                id = p.Id,
                code = p.Code,
                name = p.Name,
                seoName = p.SeoName,
                price = p.CallForPrice ? (double?)null : p.Price,
                currency = p.CurrencyCode,
                brand = p.BrandName,
                inStock = p.InStock,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// Get detailed product information by SEO name.
    /// </summary>
    [McpServerTool(Name = "get_product"), Description("Get detailed product information including variants, specifications, and media.")]
    public static async Task<string> GetProductAsync(
        ProductQueryService.ProductQueryServiceClient client,
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

        var result = new
        {
            id = product.Id,
            code = product.Code,
            name = product.Name,
            seoName = product.SeoName,
            description = product.Description,
            price = product.CallForPrice ? (double?)null : product.Price,
            currency = product.CurrencyCode,
            brand = product.Brand?.Name,
            manufacturer = product.Manufacturer?.Name,
            categories = product.Categories.Select(static c => new { name = c.CategoryName, seoName = c.CategorySeoName }),
            tags = product.Tags.Select(static t => t.Name),
            specifications = product.Specifications.Select(static g => new
            {
                group = g.Name,
                items = g.Items.Select(static i => new
                {
                    attribute = i.Name,
                    value = i.Value,
                    unit = i.Unit,
                }),
            }),
            variants = product.VariantCombinations.Select(static v => new
            {
                sku = v.Sku,
                priceAdjustment = v.PriceAdjustment,
                stockQuantity = v.StockQuantity,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// List all brands in a catalog.
    /// </summary>
    [McpServerTool(Name = "list_brands"), Description("List all brands in the catalog with product counts.")]
    public static async Task<string> ListBrandsAsync(
        BrandQueryService.BrandQueryServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetBrandsAsync(
            new GetBrandsRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
            },
            cancellationToken: cancellationToken);

        var result = new
        {
            brands = response.Brands.Select(static b => new
            {
                name = b.Name,
                seoName = b.SeoName,
                description = b.Description,
                productCount = b.ProductCount,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// Get category tree for a catalog.
    /// </summary>
    [McpServerTool(Name = "list_categories"), Description("Get the category tree for a catalog with optional depth limit.")]
    public static async Task<string> ListCategoriesAsync(
        CategoryQueryService.CategoryQueryServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        [Description("Maximum tree depth (0 = unlimited)")] int maxDepth = 0,
        CancellationToken cancellationToken = default)
    {
        var request = new GetCategoryTreeRequest
        {
            CatalogSeoName = catalogSeoName,
            LanguageCode = languageCode,
        };

        if (maxDepth > 0)
        {
            request.MaxDepth = maxDepth;
        }

        var response = await client.GetCategoryTreeAsync(request, cancellationToken: cancellationToken);

        var result = new
        {
            categories = response.RootCategories.Select(MapCategoryNode),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// List available catalogs.
    /// </summary>
    [McpServerTool(Name = "list_catalogs"), Description("List all available product catalogs.")]
    public static async Task<string> ListCatalogsAsync(
        CatalogQueryService.CatalogQueryServiceClient client,
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCatalogsAsync(
            new GetCatalogsRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            catalogs = response.Catalogs.Select(static c => new
            {
                name = c.Name,
                seoName = c.SeoName,
                productCount = c.ProductCount,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private static object MapCategoryNode(CategoryNode node) => new
    {
        name = node.Name,
        seoName = node.SeoName,
        productCount = node.ProductCount,
        children = node.Children.Select(MapCategoryNode),
    };
}
