using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.TransferOrders;

[McpServerToolType]
public static class ListTransferOrdersTool
{
    [McpServerTool(Name = "list_transfer_orders"), Description(
        "List transfer orders with optional filtering by originStoreId, destinationStoreId, or status. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TransferOrderService.TransferOrderServiceClient client,
        [Description("Origin store ID (GUID, optional)")] string? originStoreId = null,
        [Description("Destination store ID (GUID, optional)")] string? destinationStoreId = null,
        [Description("Status filter (Draft, InTransit, Received, Cancelled — optional)")] string? status = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListTransferOrdersRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(originStoreId))
        {
            request.OriginStoreId = new GuidValue(originStoreId);
        }

        if (!string.IsNullOrWhiteSpace(destinationStoreId))
        {
            request.DestinationStoreId = new GuidValue(destinationStoreId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<TransferOrderStatus>(status, ignoreCase: true);
        }

        var response = await client.ListTransferOrdersAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetTransferOrderTool.MapOrder),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
