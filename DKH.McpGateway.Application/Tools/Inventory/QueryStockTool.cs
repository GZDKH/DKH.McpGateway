using DKH.InventoryService.Contracts.Inventory.Api.StockQuery.v1;

namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class QueryStockTool
{
    [McpServerTool(Name = "query_stock"), Description(
        "Query stock levels and availability. " +
        "Actions: 'get_level' (single product), 'get_levels' (bulk by product IDs JSON), 'check_availability'. " +
        "For get_level: provide productId, optional variantId, warehouseId. " +
        "For get_levels: provide productIds JSON array of {productId, variantId?, warehouseId?}. " +
        "For check_availability: provide productId, warehouseId, quantity, optional variantId.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StockQueryService.StockQueryServiceClient client,
        [Description("Action: get_level, get_levels, or check_availability")] string action,
        [Description("Product ID (GUID)")] string? productId = null,
        [Description("Variant ID (GUID, optional)")] string? variantId = null,
        [Description("Warehouse ID (GUID)")] string? warehouseId = null,
        [Description("Quantity to check (for check_availability)")] int? quantity = null,
        [Description("JSON array of product queries (for get_levels)")] string? json = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        return action.ToLowerInvariant() switch
        {
            "get_level" => await GetLevelAsync(client, productId, variantId, warehouseId, cancellationToken),
            "get_levels" => await GetLevelsAsync(client, json, cancellationToken),
            "check_availability" => await CheckAvailabilityAsync(client, productId, variantId, warehouseId, quantity, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: get_level, get_levels, or check_availability"),
        };
    }

    private static async Task<string> GetLevelAsync(
        StockQueryService.StockQueryServiceClient client,
        string? productId, string? variantId, string? warehouseId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required for get_level");
        }

        var request = new GetStockLevelRequest
        {
            ProductId = new GuidValue(productId),
        };

        if (!string.IsNullOrWhiteSpace(variantId))
        {
            request.VariantId = new GuidValue(variantId);
        }

        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            request.WarehouseId = new GuidValue(warehouseId);
        }

        var result = await client.GetStockLevelAsync(request, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(result);
    }

    private static async Task<string> GetLevelsAsync(
        StockQueryService.StockQueryServiceClient client,
        string? json, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for get_levels");
        }

        var request = McpProtoHelper.Parser.Parse<GetStockLevelsRequest>(json);
        var result = await client.GetStockLevelsAsync(request, cancellationToken: ct);
        return JsonSerializer.Serialize(new
        {
            items = result.Items.Select(i => JsonSerializer.Deserialize<JsonElement>(McpProtoHelper.Formatter.Format(i))),
            count = result.Items.Count,
        }, McpJsonDefaults.Options);
    }

    private static async Task<string> CheckAvailabilityAsync(
        StockQueryService.StockQueryServiceClient client,
        string? productId, string? variantId, string? warehouseId, int? quantity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required for check_availability");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required for check_availability");
        }

        if (!quantity.HasValue || quantity.Value <= 0)
        {
            return McpProtoHelper.FormatError("quantity must be a positive integer for check_availability");
        }

        var request = new CheckAvailabilityRequest
        {
            ProductId = new GuidValue(productId),
            WarehouseId = new GuidValue(warehouseId),
            Quantity = quantity.Value,
        };

        if (!string.IsNullOrWhiteSpace(variantId))
        {
            request.VariantId = new GuidValue(variantId);
        }

        var result = await client.CheckAvailabilityAsync(request, cancellationToken: ct);
        return JsonSerializer.Serialize(new
        {
            isAvailable = result.IsAvailable,
            quantityAvailable = result.QuantityAvailable,
        }, McpJsonDefaults.Options);
    }
}
