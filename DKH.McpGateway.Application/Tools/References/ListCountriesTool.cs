using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing all published countries.
/// </summary>
[McpServerToolType]
public static class ListCountriesTool
{
    [McpServerTool(Name = "list_countries"), Description("List all published countries with their ISO codes and translated names.")]
    public static async Task<string> ExecuteAsync(
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

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
