using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ListCustomCustomerOrdersTool
{
    [McpServerTool(Name = "list_custom_customer_orders"), Description(
        "List custom customer orders with optional workflow status filter. " +
        "Valid statuses: Unspecified, PendingApproval, Approved, DepositConfirmed, SentToFactory, Delivered, Cancelled. " +
        "Supports pagination via page and pageSize. " +
        "Customer identity and delivery address are intentionally omitted (PII).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Filter by workflow status (optional)")] string? status = null,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListCustomCustomerOrdersRequest
        {
            Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
        };

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CustomOrderWorkflowStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            request.WorkflowStatus = parsedStatus;
        }

        var response = await client.ListCustomCustomerOrdersAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetCustomCustomerOrderTool.MapCustomOrder),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? page,
            pageSize = response.Metadata?.PageSize ?? pageSize,
        }, McpJsonDefaults.Options);
    }
}
