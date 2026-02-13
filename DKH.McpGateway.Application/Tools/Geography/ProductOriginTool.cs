using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CityManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.StateProvinceManagement.v1;

namespace DKH.McpGateway.Application.Tools.Geography;

[McpServerToolType]
public static class ProductOriginTool
{
    [McpServerTool(Name = "get_product_origin"), Description("Get product manufacturing/sourcing origin with resolved country, province, and city names.")]
    public static async Task<string> ExecuteAsync(
        ProductManagementService.ProductManagementServiceClient productClient,
        CountryManagementService.CountryManagementServiceClient countryClient,
        StateProvinceManagementService.StateProvinceManagementServiceClient provinceClient,
        CityManagementService.CityManagementServiceClient cityClient,
        [Description("Product SEO name or slug")] string productSeoName,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var product = await productClient.GetProductDetailAsync(
            new GetProductDetailRequest
            {
                CatalogSeoName = catalogSeoName,
                ProductSeoName = productSeoName,
                LanguageCode = languageCode,
            },
            cancellationToken: cancellationToken);

        if (product.Origin is null || string.IsNullOrEmpty(product.Origin.CountryCode))
        {
            return JsonSerializer.Serialize(new
            {
                productName = product.Name,
                productSeoName = product.SeoName,
                origin = (object?)null,
                message = "No origin information available for this product",
            }, McpJsonDefaults.Options);
        }

        var origin = product.Origin;
        var langCode = languageCode.Contains('-') ? languageCode : $"{languageCode}-RU";

        var countryTask = countryClient.GetAsync(
            new GetCountryRequest { Code = origin.CountryCode, Language = langCode },
            cancellationToken: cancellationToken).ResponseAsync;

        Task<StateProvinceModel>? provinceTask = null;
        if (origin.HasStateProvinceCode)
        {
            provinceTask = provinceClient.GetAsync(
                new GetStateProvinceRequest
                {
                    Code = origin.StateProvinceCode,
                    Language = langCode,
                },
                cancellationToken: cancellationToken).ResponseAsync;
        }

        Task<CityModel>? cityTask = null;
        if (origin.HasStateProvinceCode && origin.HasCityCode)
        {
            cityTask = cityClient.GetAsync(
                new GetCityRequest
                {
                    Code = origin.CityCode,
                    Language = langCode,
                },
                cancellationToken: cancellationToken).ResponseAsync;
        }

        await Task.WhenAll(
            new Task[] { countryTask }
                .Concat(provinceTask is not null ? [provinceTask] : [])
                .Concat(cityTask is not null ? [cityTask] : []));

        var country = countryTask.Result;

        var result = new
        {
            productName = product.Name,
            productSeoName = product.SeoName,
            origin = new
            {
                country = new { code = origin.CountryCode, name = country.Translations.FirstOrDefault()?.Name ?? string.Empty },
                province = provinceTask is not null
                    ? new { code = origin.StateProvinceCode, name = provinceTask.Result.Translations.FirstOrDefault()?.Name ?? string.Empty }
                    : null,
                city = cityTask is not null
                    ? new { code = origin.CityCode, name = cityTask.Result.Translations.FirstOrDefault()?.Name ?? string.Empty }
                    : null,
                placeName = origin.Details?.HasPlaceName == true ? origin.Details.PlaceName : null,
                altitude = origin.Details?.HasAltitudeMin == true
                    ? new
                    {
                        min = origin.Details.AltitudeMin,
                        max = origin.Details.HasAltitudeMax ? origin.Details.AltitudeMax : (int?)null,
                        unit = origin.Details.HasAltitudeUnit ? origin.Details.AltitudeUnit : null,
                    }
                    : null,
                coordinates = origin.Details?.HasLatitude == true && origin.Details.HasLongitude
                    ? new { lat = origin.Details.Latitude, lng = origin.Details.Longitude }
                    : null,
                notes = origin.Details?.HasNotes == true ? origin.Details.Notes : null,
            },
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
