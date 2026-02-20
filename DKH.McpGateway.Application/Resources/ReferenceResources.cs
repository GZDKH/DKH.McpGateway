using DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CurrencyManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.LanguageManagement.v1;
using Microsoft.Extensions.Caching.Memory;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
public static class ReferenceResources
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    [McpServerResource(Name = "reference://countries", MimeType = "application/json")]
    [Description("All countries with ISO two-letter codes.")]
    public static async Task<string> GetCountriesAsync(
        CountryManagementService.CountryManagementServiceClient client,
        IMemoryCache cache,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"reference://countries:{languageCode}";
        return (await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var response = await client.ListAsync(
                new ListCountriesRequest { Language = languageCode, PageSize = 1000 },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                countries = response.Items.Select(static c => new
                {
                    code = c.TwoLetterCode,
                    name = c.Translations.FirstOrDefault()?.Name ?? string.Empty,
                }),
            }, McpJsonDefaults.Options);
        }))!;
    }

    [McpServerResource(Name = "reference://countries/details", MimeType = "application/json")]
    [Description("Get country details by ISO two-letter code.")]
    public static async Task<string> GetCountryByCodeAsync(
        CountryManagementService.CountryManagementServiceClient client,
        IMemoryCache cache,
        [Description("ISO two-letter country code, e.g. 'US', 'CN', 'RU'")] string countryCode,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"reference://countries/details:{countryCode}:{languageCode}";
        return (await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var data = await client.GetAsync(
                new GetCountryRequest
                {
                    Code = countryCode,
                    Language = languageCode,
                },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                code = data.TwoLetterCode,
                code3 = data.ThreeLetterCode,
                numericCode = data.NumericCode,
                name = data.Translations.FirstOrDefault()?.Name ?? string.Empty,
                nativeName = string.IsNullOrEmpty(data.NativeName) ? null : data.NativeName,
            }, McpJsonDefaults.Options);
        }))!;
    }

    [McpServerResource(Name = "reference://currencies", MimeType = "application/json")]
    [Description("All currencies with ISO codes and symbols.")]
    public static async Task<string> GetCurrenciesAsync(
        CurrencyManagementService.CurrencyManagementServiceClient client,
        IMemoryCache cache,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"reference://currencies:{languageCode}";
        return (await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var response = await client.ListAsync(
                new ListCurrenciesRequest { Language = languageCode, PageSize = 1000 },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                currencies = response.Items.Select(static c => new
                {
                    code = c.Code,
                    name = c.Translations.FirstOrDefault()?.Name ?? string.Empty,
                    symbol = c.Symbol,
                }),
            }, McpJsonDefaults.Options);
        }))!;
    }

    [McpServerResource(Name = "reference://languages", MimeType = "application/json")]
    [Description("All supported languages with culture names.")]
    public static async Task<string> GetLanguagesAsync(
        LanguageManagementService.LanguageManagementServiceClient client,
        IMemoryCache cache,
        [Description("Language code for translations")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"reference://languages:{languageCode}";
        return (await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var response = await client.ListAsync(
                new ListLanguagesRequest { Language = languageCode, PageSize = 1000 },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                languages = response.Items.Select(static l => new
                {
                    cultureName = l.CultureName,
                    name = l.Translations.FirstOrDefault()?.Name ?? string.Empty,
                }),
            }, McpJsonDefaults.Options);
        }))!;
    }
}
