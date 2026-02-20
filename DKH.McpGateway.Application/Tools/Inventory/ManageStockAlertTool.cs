using DKH.InventoryService.Contracts.Inventory.Api.LowStockAlert.v1;

namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class ManageStockAlertTool
{
    [McpServerTool(Name = "manage_stock_alert"), Description(
        "Manage low stock alerts: list, configure, or acknowledge. " +
        "For list: optional warehouseId, acknowledged filter, page, pageSize. " +
        "For configure: provide productId, warehouseId, reorderLevel, optional variantId. " +
        "For acknowledge: provide alertId.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        LowStockAlertService.LowStockAlertServiceClient client,
        [Description("Action: list, configure, or acknowledge")] string action,
        [Description("Alert ID (GUID, for acknowledge)")] string? alertId = null,
        [Description("Product ID (GUID, for configure)")] string? productId = null,
        [Description("Variant ID (GUID, optional for configure)")] string? variantId = null,
        [Description("Warehouse ID (GUID, for configure or list filter)")] string? warehouseId = null,
        [Description("Reorder threshold level (for configure)")] int? reorderLevel = null,
        [Description("Filter by acknowledged status (for list)")] bool? acknowledged = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "list" => await ListAsync(client, apiKeyContext, warehouseId, acknowledged, page, pageSize, cancellationToken),
            "configure" => await ConfigureAsync(client, apiKeyContext, productId, variantId, warehouseId, reorderLevel, cancellationToken),
            "acknowledge" => await AcknowledgeAsync(client, apiKeyContext, alertId, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: list, configure, or acknowledge"),
        };
    }

    private static async Task<string> ListAsync(
        LowStockAlertService.LowStockAlertServiceClient client,
        IApiKeyContext ctx, string? warehouseId, bool? acknowledged, int? page, int? pageSize, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);

        var request = new GetAlertsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            request.WarehouseId = new GuidValue(warehouseId);
        }

        if (acknowledged.HasValue)
        {
            request.Acknowledged = acknowledged.Value;
        }

        var result = await client.GetAlertsAsync(request, cancellationToken: ct);
        return McpProtoHelper.FormatListResponse(
            result.Items,
            result.Metadata.TotalCount,
            result.Metadata.CurrentPage,
            result.Metadata.PageSize);
    }

    private static async Task<string> ConfigureAsync(
        LowStockAlertService.LowStockAlertServiceClient client,
        IApiKeyContext ctx, string? productId, string? variantId, string? warehouseId,
        int? reorderLevel, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required for configure");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required for configure");
        }

        if (!reorderLevel.HasValue)
        {
            return McpProtoHelper.FormatError("reorderLevel is required for configure");
        }

        var request = new ConfigureAlertRequest
        {
            ProductId = new GuidValue(productId),
            WarehouseId = new GuidValue(warehouseId),
            ReorderLevel = reorderLevel.Value,
        };

        if (!string.IsNullOrWhiteSpace(variantId))
        {
            request.VariantId = new GuidValue(variantId);
        }

        var result = await client.ConfigureAlertAsync(request, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(result);
    }

    private static async Task<string> AcknowledgeAsync(
        LowStockAlertService.LowStockAlertServiceClient client,
        IApiKeyContext ctx, string? alertId, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(alertId))
        {
            return McpProtoHelper.FormatError("alertId is required for acknowledge");
        }

        await client.AcknowledgeAlertAsync(
            new AcknowledgeAlertRequest { AlertId = new GuidValue(alertId) },
            cancellationToken: ct);
        return McpProtoHelper.FormatOk();
    }
}
