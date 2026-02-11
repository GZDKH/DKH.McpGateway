using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for listing available product catalogs.
/// </summary>
[McpServerToolType]
public static class ListCatalogsTool
{
    [McpServerTool(Name = "list_catalogs"), Description("List all available product catalogs.")]
    public static async Task<string> ExecuteAsync(
        CatalogManagementService.CatalogManagementServiceClient client,
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCatalogsAsync(
            new GetStorefrontCatalogsRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            catalogs = response.Catalogs.Select(static c => new
            {
                name = c.Name,
                seoName = c.SeoName,
                productCount = c.ProductCount,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
