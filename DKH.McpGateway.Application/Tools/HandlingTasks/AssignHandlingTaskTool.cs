using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class AssignHandlingTaskTool
{
    [McpServerTool(Name = "assign_handling_task"), Description(
        "Assign a warehouse handling task to a specific user.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        HandlingTaskService.HandlingTaskServiceClient client,
        [Description("Handling task ID (GUID)")] string id,
        [Description("Assignee user ID (GUID)")] string assigneeUserId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(assigneeUserId))
        {
            return McpProtoHelper.FormatError("assigneeUserId is required");
        }

        var task = await client.AssignHandlingTaskAsync(
            new AssignHandlingTaskRequest
            {
                Id = new GuidValue(id),
                AssigneeUserId = new GuidValue(assigneeUserId),
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetHandlingTaskTool.MapTask(task), McpJsonDefaults.Options);
    }
}
