using Google.Protobuf;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportRequest;
using ImportOptions = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportOptions;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class CreateProductTool
{
    private const string ExampleJson = """
        {
          "code": "TEA-XIHU-LONGJING-SHIFENG",
          "order": 1,
          "published": true,
          "sku": "LJ-SF-2024-50G",
          "brand": "BRAND-XIHU",
          "manufacturer": "MFR-SHIFENG",
          "price": 168.00,
          "oldPrice": 198.00,
          "markAsNew": true,
          "markAsNewStartDate": "2024-03-15",
          "markAsNewEndDate": "2024-06-01",
          "translations": [
            {
              "lang": "en",
              "name": "Xihu Longjing — Shifeng Terroir",
              "description": "Hand-picked Dragon Well tea from Shifeng terroir.",
              "seo": "xihu-longjing-shifeng"
            },
            {
              "lang": "ru",
              "name": "Сиху Лунцзин — Терруар Шифэн",
              "description": "Ручной сбор чая Лунцзин с терруара Шифэн.",
              "seo": "sihu-luncjin-shifeng"
            }
          ],
          "specifications": [
            { "group": "SPEC-GROUP-PROCESSING", "attribute": "SPEC-FERMENTATION", "option": "SPEC-FERM-0", "type": "Option", "showOnPage": true, "order": 1 },
            { "group": "SPEC-GROUP-ORIGIN", "attribute": "SPEC-ALTITUDE", "type": "CustomText", "value": "300-400m", "showOnPage": true, "order": 2 },
            { "group": "SPEC-GROUP-TASTING", "attribute": "SPEC-AROMA", "option": "SPEC-AROMA-NUTTY", "type": "Option", "showOnPage": true, "order": 3 }
          ],
          "tags": [
            { "code": "TAG-TOP10-CHINA" },
            { "code": "TAG-SPRING-2024" }
          ],
          "media": [
            { "media": "products/xihu-longjing/main.jpg", "isCover": true, "order": 1, "titles": [
              { "lang": "en", "title": "Xihu Longjing dry leaves" },
              { "lang": "ru", "title": "Сиху Лунцзин — сухой лист" }
            ]}
          ],
          "tierPrices": [
            { "catalog": "CATALOG-WHOLESALE", "quantity": 10, "price": 148.00 }
          ],
          "catalogPrices": [
            { "catalog": "CATALOG-RUSSIA", "price": 12500.00, "oldPrice": 14800.00 }
          ],
          "packages": [
            { "package": "PKG-50G", "quantity": 1, "default": true }
          ],
          "origins": [
            {
              "country": "CN",
              "state": "Zhejiang",
              "place": "Shifeng, Xihu District, Hangzhou",
              "altitude": { "min": 300, "max": 400, "unit": "m" },
              "coordinates": { "lat": 30.229, "lng": 120.108 },
              "notes": "Lion Peak — Imperial grade terroir"
            }
          ],
          "related": [
            { "product": "TEA-BAI-HAO-YINZHEN", "order": 1 }
          ],
          "crossSells": [
            { "product": "TEA-TIEGUANYIN" }
          ]
        }
        """;

    [McpServerTool(Name = "create_product"), Description(
        "Create a new product in the catalog. " +
        "Provide product JSON in data-exchange format. " +
        "Reference brands, tags, catalogs, manufacturers, and specifications by their codes (not UUIDs). " +
        "Example JSON: " + ExampleJson)]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductCatalogDataExchangeClient client,
        [Description(
            "Product JSON in data-exchange format. Required fields: code, translations (lang, name). " +
            "Optional: sku, brand (code), manufacturer (code), price, oldPrice, published, order, " +
            "tags (array of {code}), specifications (array with group/attribute/option codes), " +
            "media, origins, tierPrices, catalogPrices, packages, related, crossSells")]
        string productJson,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var jsonBytes = System.Text.Encoding.UTF8.GetBytes($"[{productJson}]");

        var response = await client.ImportAsync(
            new ImportRequest
            {
                Profile = "products",
                Format = "json",
                Content = ByteString.CopyFrom(jsonBytes),
                Options = new ImportOptions
                {
                    UpdateExisting = false,
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
