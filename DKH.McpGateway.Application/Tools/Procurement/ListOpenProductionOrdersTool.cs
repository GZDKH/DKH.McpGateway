using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ListOpenProductionOrdersTool
{
    [McpServerTool(Name = "list_open_production_orders"), Description(
        "List open production purchase orders. " +
        "Returns paginated purchase orders that are currently open and in production workflow. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListOpenProductionOrdersAsync(
            new ListOpenProductionOrdersRequest
            {
                Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetPurchaseOrderTool.MapPurchaseOrder),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? page,
            pageSize = response.Metadata?.PageSize ?? pageSize,
        }, McpJsonDefaults.Options);
    }
}
