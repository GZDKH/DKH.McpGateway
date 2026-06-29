using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ListLowStockToProduceTool
{
    [McpServerTool(Name = "list_low_stock_to_produce"), Description(
        "List products that are below reorder level and need a production order. " +
        "Optionally filter by warehouse ID. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Filter by warehouse ID (GUID, optional)")] string? warehouseId = null,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListLowStockToProduceRequest
        {
            Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
        };

        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            request.WarehouseId = new GuidValue(warehouseId);
        }

        var response = await client.ListLowStockToProduceAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(MapLowStockItem),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? page,
            pageSize = response.Metadata?.PageSize ?? pageSize,
        }, McpJsonDefaults.Options);
    }

    internal static object MapLowStockItem(LowStockItemModel item) => new
    {
        productId = item.ProductId?.Value,
        variantId = item.VariantId?.Value,
        warehouseId = item.WarehouseId?.Value,
        currentQuantity = item.CurrentQuantity,
        reorderLevel = item.ReorderLevel,
        detectedAt = item.DetectedAt?.ToDateTimeOffset().ToString("O"),
    };
}
