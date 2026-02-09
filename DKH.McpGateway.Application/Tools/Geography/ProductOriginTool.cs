using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductQuery.v1;
using DKH.ReferenceService.Contracts.Api.CityQuery.V1;
using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;
using DKH.ReferenceService.Contracts.Api.StateProvinceQuery.V1;

namespace DKH.McpGateway.Application.Tools.Geography;

[McpServerToolType]
public static class ProductOriginTool
{
    [McpServerTool(Name = "get_product_origin"), Description("Get product manufacturing/sourcing origin with resolved country, province, and city names.")]
    public static async Task<string> ExecuteAsync(
        ProductQueryService.ProductQueryServiceClient productClient,
        CountryQueryService.CountryQueryServiceClient countryClient,
        StateProvinceQueryService.StateProvinceQueryServiceClient provinceClient,
        CityQueryService.CityQueryServiceClient cityClient,
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

        var countryTask = countryClient.GetCountryByCodeAsync(
            new GetCountryByCodeRequest { TwoLetterCode = origin.CountryCode, LanguageCode = langCode },
            cancellationToken: cancellationToken).ResponseAsync;

        Task<StateProvinceResponse>? provinceTask = null;
        if (origin.HasStateProvinceCode)
        {
            provinceTask = provinceClient.GetStateProvinceByCodeAsync(
                new GetStateProvinceByCodeRequest
                {
                    CountryCode = origin.CountryCode,
                    StateCode = origin.StateProvinceCode,
                    LanguageCode = langCode,
                },
                cancellationToken: cancellationToken).ResponseAsync;
        }

        Task<CityResponse>? cityTask = null;
        if (origin.HasStateProvinceCode && origin.HasCityCode)
        {
            cityTask = cityClient.GetCityByCodeAsync(
                new GetCityByCodeRequest
                {
                    CountryCode = origin.CountryCode,
                    StateCode = origin.StateProvinceCode,
                    CityCode = origin.CityCode,
                    LanguageCode = langCode,
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
                country = new { code = origin.CountryCode, name = country.Name },
                province = provinceTask is not null
                    ? new { code = origin.StateProvinceCode, name = provinceTask.Result.Name }
                    : null,
                city = cityTask is not null
                    ? new { code = origin.CityCode, name = cityTask.Result.Name }
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
