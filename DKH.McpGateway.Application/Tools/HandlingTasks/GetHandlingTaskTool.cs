using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class GetHandlingTaskTool
{
    [McpServerTool(Name = "get_handling_task"), Description(
        "Get a single warehouse handling task by its ID. " +
        "Returns task details including type, status, priority, assignment, and timestamps.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        HandlingTaskService.HandlingTaskServiceClient client,
        [Description("Handling task ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var task = await client.GetHandlingTaskAsync(
            new GetHandlingTaskRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapTask(task), McpJsonDefaults.Options);
    }

    internal static object MapTask(HandlingTaskModel t) => new
    {
        id = t.Id?.Value,
        warehouseId = t.WarehouseId?.Value,
        zoneId = t.ZoneId?.Value,
        type = t.Type.ToString(),
        status = t.Status.ToString(),
        priority = t.Priority.ToString(),
        assigneeUserId = t.AssigneeUserId?.Value,
        dueAt = t.DueAt?.ToDateTimeOffset().ToString("O"),
        externalRef = t.ExternalRef,
        notes = t.Notes,
        startedAt = t.StartedAt?.ToDateTimeOffset().ToString("O"),
        completedAt = t.CompletedAt?.ToDateTimeOffset().ToString("O"),
        cancelledAt = t.CancelledAt?.ToDateTimeOffset().ToString("O"),
        cancellationReason = t.CancellationReason,
        createdAt = t.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = t.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
