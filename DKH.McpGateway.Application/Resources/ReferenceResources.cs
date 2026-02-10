using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;
using DKH.ReferenceService.Contracts.Api.CurrencyQuery.V1;
using DKH.ReferenceService.Contracts.Api.LanguageQuery.V1;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
public static class ReferenceResources
{
    [McpServerResource(Name = "reference://countries", MimeType = "application/json")]
    [Description("All countries with ISO two-letter codes.")]
    public static async Task<string> GetCountriesAsync(
        CountryQueryService.CountryQueryServiceClient client,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCountriesAsync(
            new GetCountriesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            countries = response.Countries.Select(static c => new
            {
                code = c.TwoLetterCode,
                name = c.Name,
            }),
        }, McpJsonDefaults.Options);
    }

    [McpServerResource(Name = "reference://countries/details", MimeType = "application/json")]
    [Description("Get country details by ISO two-letter code.")]
    public static async Task<string> GetCountryByCodeAsync(
        CountryQueryService.CountryQueryServiceClient client,
        [Description("ISO two-letter country code, e.g. 'US', 'CN', 'RU'")] string countryCode,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCountryByCodeAsync(
            new GetCountryByCodeRequest
            {
                TwoLetterCode = countryCode,
                LanguageCode = languageCode,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            code = response.TwoLetterCode,
            code3 = response.ThreeLetterCode,
            numericCode = response.NumericCode,
            name = response.Name,
            nativeName = string.IsNullOrEmpty(response.NativeName) ? null : response.NativeName,
        }, McpJsonDefaults.Options);
    }

    [McpServerResource(Name = "reference://currencies", MimeType = "application/json")]
    [Description("All currencies with ISO codes and symbols.")]
    public static async Task<string> GetCurrenciesAsync(
        CurrencyQueryService.CurrencyQueryServiceClient client,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCurrenciesAsync(
            new GetCurrenciesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            currencies = response.Currencies.Select(static c => new
            {
                code = c.Code,
                name = c.Name,
                symbol = c.Symbol,
            }),
        }, McpJsonDefaults.Options);
    }

    [McpServerResource(Name = "reference://languages", MimeType = "application/json")]
    [Description("All supported languages with culture names.")]
    public static async Task<string> GetLanguagesAsync(
        LanguageQueryService.LanguageQueryServiceClient client,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetLanguagesAsync(
            new GetLanguagesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            languages = response.Languages.Select(static l => new
            {
                cultureName = l.CultureName,
                name = l.Name,
            }),
        }, McpJsonDefaults.Options);
    }
}
