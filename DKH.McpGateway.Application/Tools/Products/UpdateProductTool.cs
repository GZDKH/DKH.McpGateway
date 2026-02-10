using Google.Protobuf;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportRequest;
using ImportOptions = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportOptions;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class UpdateProductTool
{
    private const string ExampleJson = """
        {
          "code": "TEA-XIHU-LONGJING-SHIFENG",
          "price": 178.00,
          "oldPrice": 198.00,
          "published": true,
          "translations": [
            {
              "lang": "en",
              "name": "Xihu Longjing — Shifeng Terroir (Updated)",
              "description": "Updated description for Dragon Well tea.",
              "seo": "xihu-longjing-shifeng"
            },
            {
              "lang": "ru",
              "name": "Сиху Лунцзин — Терруар Шифэн (Обновлён)",
              "description": "Обновлённое описание чая Лунцзин.",
              "seo": "sihu-luncjin-shifeng"
            }
          ],
          "tags": [
            { "code": "TAG-TOP10-CHINA" },
            { "code": "TAG-LIMITED-EDITION" }
          ],
          "specifications": [
            { "group": "SPEC-GROUP-PROCESSING", "attribute": "SPEC-FERMENTATION", "option": "SPEC-FERM-0", "type": "Option", "showOnPage": true, "order": 1 }
          ],
          "origins": [
            {
              "country": "CN",
              "state": "Zhejiang",
              "place": "Shifeng, Xihu District, Hangzhou",
              "altitude": { "min": 300, "max": 400, "unit": "m" }
            }
          ]
        }
        """;

    [McpServerTool(Name = "update_product"), Description(
        "Update an existing product in the catalog. " +
        "The product is identified by its code field. " +
        "Provide only the fields you want to update (code is always required for identification). " +
        "Reference brands, tags, catalogs, and specifications by their codes. " +
        "Example JSON: " + ExampleJson)]
    public static async Task<string> ExecuteAsync(
        ProductCatalogDataExchangeClient client,
        [Description(
            "Product JSON in data-exchange format. 'code' is required to identify the product. " +
            "Include only fields to update: price, oldPrice, published, translations, " +
            "tags, specifications, media, origins, tierPrices, catalogPrices, packages, related, crossSells")]
        string productJson,
        CancellationToken cancellationToken = default)
    {
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes($"[{productJson}]");

        var response = await client.ImportAsync(
            new ImportRequest
            {
                Profile = "products",
                Format = "json",
                Content = ByteString.CopyFrom(jsonBytes),
                Options = new ImportOptions
                {
                    UpdateExisting = true,
                    SkipErrors = false,
                    CreateMissingGroups = true,
                    CreateMissingAttributes = true,
                    CreateMissingOptions = true,
                },
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            success = response.Failed == 0,
            processed = response.Processed,
            failed = response.Failed,
            errors = response.Errors,
        }, McpJsonDefaults.Options);
    }
}
