using DKH.ReferenceService.Contracts.Api.CurrencyQuery.V1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing all published currencies.
/// </summary>
[McpServerToolType]
public static class ListCurrenciesTool
{
    [McpServerTool(Name = "list_currencies"), Description("List all published currencies with their ISO codes and symbols.")]
    public static async Task<string> ExecuteAsync(
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

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
