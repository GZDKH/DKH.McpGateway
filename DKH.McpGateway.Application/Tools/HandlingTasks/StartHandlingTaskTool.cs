using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class StartHandlingTaskTool
{
    [McpServerTool(Name = "start_handling_task"), Description(
        "Start a warehouse handling task, transitioning it from Pending to InProgress.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        HandlingTaskService.HandlingTaskServiceClient client,
        [Description("Handling task ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var task = await client.StartHandlingTaskAsync(
            new StartHandlingTaskRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetHandlingTaskTool.MapTask(task), McpJsonDefaults.Options);
    }
}
