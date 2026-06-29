using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ListAssignedInspectionsTool
{
    [McpServerTool(Name = "list_assigned_inspections"), Description(
        "List inspections assigned to a specific inspector. " +
        "Filter by inspector ID and optionally by status. " +
        "Valid statuses: Unspecified, Scheduled, InProgress, Completed, Cancelled. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspector reference ID (GUID)")] string inspectorId,
        [Description("Filter by inspection status (optional)")] string? status = null,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(inspectorId))
        {
            return McpProtoHelper.FormatError("inspectorId is required");
        }

        var request = new ListAssignedInspectionsRequest
        {
            InspectorRef = new GuidValue(inspectorId),
            Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
        };

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<InspectionStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            request.Status = parsedStatus;
        }

        var response = await client.ListAssignedInspectionsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetInspectionTool.MapInspection),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? page,
            pageSize = response.Metadata?.PageSize ?? pageSize,
        }, McpJsonDefaults.Options);
    }
}
