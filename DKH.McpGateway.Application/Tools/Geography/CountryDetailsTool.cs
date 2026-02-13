using DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.StateProvinceManagement.v1;

namespace DKH.McpGateway.Application.Tools.Geography;

[McpServerToolType]
public static class CountryDetailsTool
{
    [McpServerTool(Name = "get_country_details"), Description("Get country details with its provinces/regions. Useful for understanding product origins and regional structure.")]
    public static async Task<string> ExecuteAsync(
        CountryManagementService.CountryManagementServiceClient countryClient,
        StateProvinceManagementService.StateProvinceManagementServiceClient provinceClient,
        [Description("ISO 2-letter country code (e.g. 'CN', 'US', 'DE')")] string countryCode,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var countryTask = countryClient.GetAsync(
            new GetCountryRequest { Code = countryCode, Language = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        var provincesTask = provinceClient.ListAsync(
            new ListStateProvincesRequest { Language = languageCode, PageSize = 10000 },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(countryTask, provincesTask);

        var country = countryTask.Result;
        var allProvinces = provincesTask.Result.Items;

        var filtered = allProvinces
            .Where(p => p.Country.Equals(countryCode, StringComparison.OrdinalIgnoreCase));

        var result = new
        {
            code = country.TwoLetterCode,
            code3 = country.ThreeLetterCode,
            name = country.Translations.FirstOrDefault()?.Name ?? string.Empty,
            nativeName = string.IsNullOrEmpty(country.NativeName) ? null : country.NativeName,
            provinces = filtered.Select(static p => new
            {
                code = p.Code,
                name = p.Translations.FirstOrDefault()?.Name ?? string.Empty,
            }),
            provinceCount = filtered.Count(),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
