using System.ComponentModel;
using System.Text.Json;
using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;
using DKH.ReferenceService.Contracts.Api.CurrencyQuery.V1;
using DKH.ReferenceService.Contracts.Api.DeliveryQuery.V1;
using DKH.ReferenceService.Contracts.Api.LanguageQuery.V1;
using DKH.ReferenceService.Contracts.Api.MeasurementQuery.V1;
using ModelContextProtocol.Server;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tools for querying reference data (countries, currencies, languages, measurements, delivery times).
/// </summary>
[McpServerToolType]
public static class ReferenceTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// List all published countries.
    /// </summary>
    [McpServerTool(Name = "list_countries"), Description("List all published countries with their ISO codes and translated names.")]
    public static async Task<string> ListCountriesAsync(
        CountryQueryService.CountryQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCountriesAsync(
            new GetCountriesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            countries = response.Countries.Select(static c => new
            {
                code = c.TwoLetterCode,
                name = c.Name,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// List all published currencies.
    /// </summary>
    [McpServerTool(Name = "list_currencies"), Description("List all published currencies with their ISO codes and symbols.")]
    public static async Task<string> ListCurrenciesAsync(
        CurrencyQueryService.CurrencyQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCurrenciesAsync(
            new GetCurrenciesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            currencies = response.Currencies.Select(static c => new
            {
                code = c.Code,
                name = c.Name,
                symbol = c.Symbol,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// List all published languages.
    /// </summary>
    [McpServerTool(Name = "list_languages"), Description("List all published languages with their culture codes.")]
    public static async Task<string> ListLanguagesAsync(
        LanguageQueryService.LanguageQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetLanguagesAsync(
            new GetLanguagesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            languages = response.Languages.Select(static l => new
            {
                cultureName = l.CultureName,
                name = l.Name,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// List measurement units (weights and dimensions).
    /// </summary>
    [McpServerTool(Name = "list_measurements"), Description("List measurement units: weights and dimensions with their conversion ratios.")]
    public static async Task<string> ListMeasurementsAsync(
        MeasurementQueryService.MeasurementQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var weightsTask = client.GetWeightsAsync(
            new GetWeightsRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        var dimensionsTask = client.GetDimensionsAsync(
            new GetDimensionsRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(weightsTask, dimensionsTask);

        var result = new
        {
            weights = weightsTask.Result.Weights.Select(static w => new
            {
                code = w.Code,
                name = w.Name,
                ratioToPrimary = w.RatioToPrimary,
            }),
            dimensions = dimensionsTask.Result.Dimensions.Select(static d => new
            {
                code = d.Code,
                name = d.Name,
                ratioToPrimary = d.RatioToPrimary,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// List delivery time options.
    /// </summary>
    [McpServerTool(Name = "list_delivery_times"), Description("List available delivery time options with estimated day ranges.")]
    public static async Task<string> ListDeliveryTimesAsync(
        DeliveryQueryService.DeliveryQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetDeliveryTimesAsync(
            new GetDeliveryTimesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            deliveryTimes = response.DeliveryTimes.Select(static d => new
            {
                code = d.Code,
                name = d.Name,
                color = d.Color,
                minDays = d.MinDays,
                maxDays = d.MaxDays,
            }),
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }
}
