using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;
using DKH.ReferenceService.Contracts.Api.StateProvinceQuery.V1;

namespace DKH.McpGateway.Application.Tools.Geography;

[McpServerToolType]
public static class CountryDetailsTool
{
    [McpServerTool(Name = "get_country_details"), Description("Get country details with its provinces/regions. Useful for understanding product origins and regional structure.")]
    public static async Task<string> ExecuteAsync(
        CountryQueryService.CountryQueryServiceClient countryClient,
        StateProvinceQueryService.StateProvinceQueryServiceClient provinceClient,
        [Description("ISO 2-letter country code (e.g. 'CN', 'US', 'DE')")] string countryCode,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var countryTask = countryClient.GetCountryByCodeAsync(
            new GetCountryByCodeRequest { TwoLetterCode = countryCode, LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        var provincesTask = provinceClient.GetStateProvincesAsync(
            new GetStateProvincesRequest { CountryCode = countryCode, LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(countryTask, provincesTask);

        var country = countryTask.Result;
        var provinces = provincesTask.Result;

        var result = new
        {
            code = country.TwoLetterCode,
            code3 = country.ThreeLetterCode,
            name = country.Name,
            nativeName = string.IsNullOrEmpty(country.NativeName) ? null : country.NativeName,
            provinces = provinces.StateProvinces.Select(static p => new
            {
                code = p.Code,
                name = p.Name,
            }),
            provinceCount = provinces.StateProvinces.Count,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
