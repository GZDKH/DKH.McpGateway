using DKH.InventoryService.Contracts.Inventory.Api.StockManagement.v1;

namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class ManageStockTool
{
    [McpServerTool(Name = "manage_stock"), Description(
        "Manage stock levels: set, adjust, or get warehouse stock. " +
        "For set_level: provide productId, warehouseId, quantity, reorderLevel, optional variantId. " +
        "For adjust: provide productId, warehouseId, adjustment (positive/negative), optional variantId, reason. " +
        "For get_warehouse_stock: provide warehouseId, optional page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StockManagementService.StockManagementServiceClient client,
        [Description("Action: set_level, adjust, or get_warehouse_stock")] string action,
        [Description("Product ID (GUID)")] string? productId = null,
        [Description("Variant ID (GUID, optional)")] string? variantId = null,
        [Description("Warehouse ID (GUID)")] string? warehouseId = null,
        [Description("Stock quantity (for set_level)")] int? quantity = null,
        [Description("Reorder threshold level (for set_level)")] int? reorderLevel = null,
        [Description("Adjustment amount, positive or negative (for adjust)")] int? adjustment = null,
        [Description("Reason for adjustment (for adjust)")] string? reason = null,
        [Description("Page number (for get_warehouse_stock, default 1)")] int? page = null,
        [Description("Page size (for get_warehouse_stock, default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "set_level" => await SetLevelAsync(client, apiKeyContext, productId, variantId, warehouseId, quantity, reorderLevel, cancellationToken),
            "adjust" => await AdjustAsync(client, apiKeyContext, productId, variantId, warehouseId, adjustment, reason, cancellationToken),
            "get_warehouse_stock" => await GetWarehouseStockAsync(client, apiKeyContext, warehouseId, page, pageSize, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: set_level, adjust, or get_warehouse_stock"),
        };
    }

    private static async Task<string> SetLevelAsync(
        StockManagementService.StockManagementServiceClient client,
        IApiKeyContext ctx, string? productId, string? variantId, string? warehouseId,
        int? quantity, int? reorderLevel, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required for set_level");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required for set_level");
        }

        if (!quantity.HasValue)
        {
            return McpProtoHelper.FormatError("quantity is required for set_level");
        }

        var request = new SetStockLevelRequest
        {
            ProductId = new GuidValue(productId),
            WarehouseId = new GuidValue(warehouseId),
            Quantity = quantity.Value,
        };

        if (!string.IsNullOrWhiteSpace(variantId))
        {
            request.VariantId = new GuidValue(variantId);
        }

        if (reorderLevel.HasValue)
        {
            request.ReorderLevel = reorderLevel.Value;
        }

        var result = await client.SetStockLevelAsync(request, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(result);
    }

    private static async Task<string> AdjustAsync(
        StockManagementService.StockManagementServiceClient client,
        IApiKeyContext ctx, string? productId, string? variantId, string? warehouseId,
        int? adjustment, string? reason, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required for adjust");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required for adjust");
        }

        if (!adjustment.HasValue)
        {
            return McpProtoHelper.FormatError("adjustment is required for adjust");
        }

        var request = new AdjustStockRequest
        {
            ProductId = new GuidValue(productId),
            WarehouseId = new GuidValue(warehouseId),
            Adjustment = adjustment.Value,
        };

        if (!string.IsNullOrWhiteSpace(variantId))
        {
            request.VariantId = new GuidValue(variantId);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            request.Reason = reason;
        }

        var result = await client.AdjustStockAsync(request, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(result);
    }

    private static async Task<string> GetWarehouseStockAsync(
        StockManagementService.StockManagementServiceClient client,
        IApiKeyContext ctx, string? warehouseId, int? page, int? pageSize, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required for get_warehouse_stock");
        }

        var request = new GetWarehouseStockRequest
        {
            WarehouseId = new GuidValue(warehouseId),
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        var result = await client.GetWarehouseStockAsync(request, cancellationToken: ct);
        return McpProtoHelper.FormatListResponse(
            result.Items,
            result.Metadata.TotalCount,
            result.Metadata.CurrentPage,
            result.Metadata.PageSize);
    }
}
