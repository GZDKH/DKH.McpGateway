using DKH.ReferenceService.Contracts.Api.LanguageQuery.V1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing all published languages.
/// </summary>
[McpServerToolType]
public static class ListLanguagesTool
{
    [McpServerTool(Name = "list_languages"), Description("List all published languages with their culture codes.")]
    public static async Task<string> ExecuteAsync(
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

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
