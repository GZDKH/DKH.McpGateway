using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ListInspectionsTool
{
    [McpServerTool(Name = "list_inspections"), Description(
        "List inspections with optional filtering by status or purchase order. " +
        "Valid statuses: Unspecified, Scheduled, InProgress, Completed, Cancelled. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Filter by inspection status (optional)")] string? status = null,
        [Description("Filter by purchase order ID (GUID, optional)")] string? purchaseOrderId = null,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListInspectionsRequest
        {
            Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
        };

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<InspectionStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            request.Status = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(purchaseOrderId))
        {
            request.PurchaseOrderId = new GuidValue(purchaseOrderId);
        }

        var response = await client.ListInspectionsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetInspectionTool.MapInspection),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? page,
            pageSize = response.Metadata?.PageSize ?? pageSize,
        }, McpJsonDefaults.Options);
    }
}
