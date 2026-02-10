using Google.Protobuf;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoriesCrud.v1;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportRequest;
using ImportOptions = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportOptions;

namespace DKH.McpGateway.Application.Tools.Categories;

[McpServerToolType]
public static class ManageCategoryTool
{
    private const string ExampleJson = """
        {
          "code": "CAT-GREEN-TEA",
          "order": 1,
          "published": true,
          "translations": [
            { "lang": "en", "name": "Green Tea", "description": "Unfermented tea (0%)", "seo": "green-tea" },
            { "lang": "ru", "name": "Зелёный чай", "description": "Неферментированный чай (0%)", "seo": "zelenyj-chaj" },
            { "lang": "zh", "name": "绿茶", "description": "非发酵茶" }
          ]
        }
        """;

    [McpServerTool(Name = "manage_category"), Description(
        "Create, update, or delete a product category. " +
        "For create/update: provide category JSON in data-exchange format with code, translations (lang, name, description, seo). " +
        "For delete: provide the category code. " +
        "Example JSON: " + ExampleJson)]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductCatalogDataExchangeClient exchangeClient,
        CategoriesCrudService.CategoriesCrudServiceClient crudClient,
        [Description("Action: create, update, or delete")] string action,
        [Description("Category JSON in data-exchange format (for create/update)")] string? categoryJson = null,
        [Description("Category code (for delete, e.g. 'CAT-GREEN-TEA')")] string? categoryCode = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(categoryJson))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "categoryJson is required for create/update" },
                    McpJsonDefaults.Options);
            }

            var jsonBytes = System.Text.Encoding.UTF8.GetBytes($"[{categoryJson}]");

            var response = await exchangeClient.ImportAsync(
                new ImportRequest
                {
                    Profile = "categories",
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
            if (string.IsNullOrEmpty(categoryCode))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "categoryCode is required for delete" },
                    McpJsonDefaults.Options);
            }

            var categories = await crudClient.GetCategoriesAsync(
                new GetCategoriesRequest
                {
                    Search = categoryCode,
                    Page = 1,
                    PageSize = 10,
                },
                cancellationToken: cancellationToken);

            var category = categories.Categories.FirstOrDefault(c =>
                string.Equals(c.Code, categoryCode, StringComparison.OrdinalIgnoreCase));

            if (category is null)
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = $"Category with code '{categoryCode}' not found" },
                    McpJsonDefaults.Options);
            }

            await crudClient.DeleteCategoryAsync(
                new DeleteCategoryRequest { CategoryId = category.Id },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedCode = category.Code,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }
}
