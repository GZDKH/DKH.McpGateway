using Google.Protobuf;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.TagsCrud.v1;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportRequest;
using ImportOptions = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportOptions;

namespace DKH.McpGateway.Application.Tools.Tags;

[McpServerToolType]
public static class ManageTagsTool
{
    private const string ExampleJson = """
        {
          "code": "TAG-UNESCO",
          "order": 2,
          "published": true,
          "translations": [
            { "lang": "en", "name": "UNESCO Heritage", "description": "UNESCO Intangible Cultural Heritage 2022" },
            { "lang": "ru", "name": "Наследие UNESCO", "description": "Нематериальное культурное наследие UNESCO 2022" },
            { "lang": "zh", "name": "UNESCO遗产", "description": "联合国教科文组织非物质文化遗产" }
          ]
        }
        """;

    [McpServerTool(Name = "manage_tags"), Description(
        "Create, update, or delete product tags. " +
        "For create/update: provide tag JSON in data-exchange format with code, translations (lang, name, description). " +
        "For delete: provide the tag name to search and delete. " +
        "Example JSON: " + ExampleJson)]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductCatalogDataExchangeClient exchangeClient,
        TagsCrudService.TagsCrudServiceClient crudClient,
        [Description("Action: create, update, or delete")] string action,
        [Description("Tag JSON in data-exchange format (for create/update)")] string? tagJson = null,
        [Description("Tag name to find for delete (e.g. 'UNESCO Heritage' or 'Наследие UNESCO')")] string? tagName = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(tagJson))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "tagJson is required for create/update" },
                    McpJsonDefaults.Options);
            }

            var jsonBytes = System.Text.Encoding.UTF8.GetBytes($"[{tagJson}]");

            var response = await exchangeClient.ImportAsync(
                new ImportRequest
                {
                    Profile = "tags",
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
            if (string.IsNullOrEmpty(tagName))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "tagName is required for delete" },
                    McpJsonDefaults.Options);
            }

            var tags = await crudClient.GetTagsAsync(
                new GetTagsRequest
                {
                    NameContains = tagName,
                    Page = 1,
                    PageSize = 10,
                },
                cancellationToken: cancellationToken);

            var tag = tags.Tags.FirstOrDefault(t =>
                t.Translations.Any(tr =>
                    string.Equals(tr.Name, tagName, StringComparison.OrdinalIgnoreCase)));

            if (tag is null && tags.Tags.Count == 1)
            {
                tag = tags.Tags[0];
            }

            if (tag is null)
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = $"Tag '{tagName}' not found" },
                    McpJsonDefaults.Options);
            }

            await crudClient.DeleteTagAsync(
                new DeleteTagRequest { TagId = tag.Id },
                cancellationToken: cancellationToken);

            var deletedName = tag.Translations.FirstOrDefault()?.Name ?? tag.Id;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedTag = deletedName,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }
}
