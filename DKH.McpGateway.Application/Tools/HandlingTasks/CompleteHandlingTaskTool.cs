using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class CompleteHandlingTaskTool
{
    [McpServerTool(Name = "complete_handling_task"), Description(
        "Complete a warehouse handling task that is InProgress.")]
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

        var task = await client.CompleteHandlingTaskAsync(
            new CompleteHandlingTaskRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetHandlingTaskTool.MapTask(task), McpJsonDefaults.Options);
    }
}
