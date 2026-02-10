using Google.Protobuf;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandsCrud.v1;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportRequest;
using ImportOptions = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportOptions;

namespace DKH.McpGateway.Application.Tools.Brands;

[McpServerToolType]
public static class ManageBrandTool
{
    private const string ExampleJson = """
        {
          "code": "BRAND-XIHU",
          "order": 1,
          "published": true,
          "translations": [
            { "lang": "en", "name": "Xihu (West Lake)", "description": "Origin of Dragon Well tea", "seo": "xihu-west-lake" },
            { "lang": "ru", "name": "Сиху (Западное озеро)", "description": "Родина чая Лунцзин", "seo": "sikhu-zapadnoe-ozero" },
            { "lang": "zh", "name": "西湖", "description": "龙井茶原产地" }
          ]
        }
        """;

    [McpServerTool(Name = "manage_brand"), Description(
        "Create, update, or delete a brand. " +
        "For create/update: provide brand JSON in data-exchange format with code, translations (lang, name, description, seo). " +
        "For delete: provide the brand name to search and delete. " +
        "Example JSON: " + ExampleJson)]
    public static async Task<string> ExecuteAsync(
        ProductCatalogDataExchangeClient exchangeClient,
        BrandsCrudService.BrandsCrudServiceClient crudClient,
        [Description("Action: create, update, or delete")] string action,
        [Description("Brand JSON in data-exchange format (for create/update)")] string? brandJson = null,
        [Description("Brand name or code to find for delete (e.g. 'Xihu (West Lake)' or 'BRAND-XIHU')")] string? brandName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(brandJson))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "brandJson is required for create/update" },
                    McpJsonDefaults.Options);
            }

            var jsonBytes = System.Text.Encoding.UTF8.GetBytes($"[{brandJson}]");

            var response = await exchangeClient.ImportAsync(
                new ImportRequest
                {
                    Profile = "brands",
                    Format = "json",
                    Content = ByteString.CopyFrom(jsonBytes),
                    Options = new ImportOptions
                    {
                        UpdateExisting = string.Equals(action, "update", StringComparison.OrdinalIgnoreCase),
                        SkipErrors = false,
                    },
                },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Failed == 0,
                action,
                processed = response.Processed,
                failed = response.Failed,
                errors = response.Errors,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(brandName))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "brandName is required for delete" },
                    McpJsonDefaults.Options);
            }

            var brands = await crudClient.GetBrandsAsync(
                new GetBrandsRequest
                {
                    Search = brandName,
                    Page = 1,
                    PageSize = 10,
                },
                cancellationToken: cancellationToken);

            var brand = brands.Brands.FirstOrDefault(b =>
                b.Translations.Any(t =>
                    string.Equals(t.Name, brandName, StringComparison.OrdinalIgnoreCase)));

            if (brand is null && brands.Brands.Count == 1)
            {
                brand = brands.Brands[0];
            }

            if (brand is null)
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = $"Brand '{brandName}' not found" },
                    McpJsonDefaults.Options);
            }

            await crudClient.DeleteBrandAsync(
                new DeleteBrandRequest { BrandId = brand.Id },
                cancellationToken: cancellationToken);

            var deletedName = brand.Translations.FirstOrDefault()?.Name ?? brand.Id;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedBrand = deletedName,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }
}
