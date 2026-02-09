using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductQuery.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for getting detailed product information by SEO name.
/// </summary>
[McpServerToolType]
public static class GetProductTool
{
    [McpServerTool(Name = "get_product"), Description("Get detailed product information including variants, specifications, and media.")]
    public static async Task<string> ExecuteAsync(
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
            origin = product.Origin is not null && !string.IsNullOrEmpty(product.Origin.CountryCode)
                ? new
                {
                    countryCode = product.Origin.CountryCode,
                    stateProvinceCode = product.Origin.HasStateProvinceCode ? product.Origin.StateProvinceCode : null,
                    cityCode = product.Origin.HasCityCode ? product.Origin.CityCode : null,
                    placeName = product.Origin.Details?.HasPlaceName == true ? product.Origin.Details.PlaceName : null,
                }
                : null,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
