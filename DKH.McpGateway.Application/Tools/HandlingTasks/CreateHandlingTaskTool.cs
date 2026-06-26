using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class CreateHandlingTaskTool
{
    [McpServerTool(Name = "create_handling_task"), Description(
        "Create a new warehouse handling task. " +
        "dueAt should be an ISO 8601 datetime string (UTC recommended).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        HandlingTaskService.HandlingTaskServiceClient client,
        [Description("Warehouse ID (GUID)")] string warehouseId,
        [Description("Task type (Receiving, Picking, Packing, Dispatch, Staging)")] string type,
        [Description("Priority (Low, Normal, High, Urgent)")] string priority,
        [Description("Zone ID (GUID, optional)")] string? zoneId = null,
        [Description("Assignee user ID (GUID, optional)")] string? assigneeUserId = null,
        [Description("Due date/time in ISO 8601 format (optional)")] string? dueAt = null,
        [Description("External reference identifier (optional)")] string? externalRef = null,
        [Description("Notes (optional)")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required");
        }

        var request = new CreateHandlingTaskRequest
        {
            WarehouseId = new GuidValue(warehouseId),
            Type = Enum.Parse<HandlingTaskType>(type, ignoreCase: true),
            Priority = Enum.Parse<HandlingTaskPriority>(priority, ignoreCase: true),
        };

        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            request.ZoneId = new GuidValue(zoneId);
        }

        if (!string.IsNullOrWhiteSpace(assigneeUserId))
        {
            request.AssigneeUserId = new GuidValue(assigneeUserId);
        }

        if (!string.IsNullOrWhiteSpace(dueAt) && DateTimeOffset.TryParse(dueAt, out var dueAtParsed))
        {
            request.DueAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dueAtParsed.UtcDateTime);
        }

        if (!string.IsNullOrWhiteSpace(externalRef))
        {
            request.ExternalRef = externalRef;
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            request.Notes = notes;
        }

        var task = await client.CreateHandlingTaskAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetHandlingTaskTool.MapTask(task), McpJsonDefaults.Options);
    }
}
