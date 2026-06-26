using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class ListHandlingTasksTool
{
    [McpServerTool(Name = "list_handling_tasks"), Description(
        "List warehouse handling tasks. " +
        "The server requires at least one of warehouseId or assigneeUserId. " +
        "Supports optional filtering by zoneId, type, status, and priority.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        HandlingTaskService.HandlingTaskServiceClient client,
        [Description("Warehouse ID (GUID, optional — server requires at least warehouseId or assigneeUserId)")] string? warehouseId = null,
        [Description("Assignee user ID (GUID, optional)")] string? assigneeUserId = null,
        [Description("Zone ID (GUID, optional)")] string? zoneId = null,
        [Description("Task type filter (Receiving, Picking, Packing, Dispatch, Staging — optional)")] string? type = null,
        [Description("Status filter (Pending, InProgress, Completed, Cancelled — optional)")] string? status = null,
        [Description("Priority filter (Low, Normal, High, Urgent — optional)")] string? priority = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListHandlingTasksRequest
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

        if (!string.IsNullOrWhiteSpace(assigneeUserId))
        {
            request.AssigneeUserId = new GuidValue(assigneeUserId);
        }

        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            request.ZoneId = new GuidValue(zoneId);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            request.Type = Enum.Parse<HandlingTaskType>(type, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<HandlingTaskStatus>(status, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            request.Priority = Enum.Parse<HandlingTaskPriority>(priority, ignoreCase: true);
        }

        var response = await client.ListHandlingTasksAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetHandlingTaskTool.MapTask),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
