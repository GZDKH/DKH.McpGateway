using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;

namespace DKH.McpGateway.Application.Tools.HandlingTasks;

[McpServerToolType]
public static class CancelHandlingTaskTool
{
    [McpServerTool(Name = "cancel_handling_task"), Description(
        "Cancel a warehouse handling task.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        HandlingTaskService.HandlingTaskServiceClient client,
        [Description("Handling task ID (GUID)")] string id,
        [Description("Reason for cancellation (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new CancelHandlingTaskRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            request.Reason = reason;
        }

        var task = await client.CancelHandlingTaskAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetHandlingTaskTool.MapTask(task), McpJsonDefaults.Options);
    }
}
