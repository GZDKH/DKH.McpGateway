using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using NotificationGrpc = DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1.NotificationManagementService;

namespace DKH.McpGateway.Application.Tools.Notifications;

[McpServerToolType]
public static class GetBulkJobStatusTool
{
    [McpServerTool(Name = "get_bulk_job_status"), Description(
        "Get read-only bulk notification job status and counters by job ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NotificationGrpc.NotificationManagementServiceClient client,
        [Description("Bulk notification job ID (GUID)")] string jobId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return McpProtoHelper.FormatError("jobId is required");
        }

        var response = await client.GetBulkJobStatusAsync(
            new GetBulkJobStatusRequest { JobId = new GuidValue(jobId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(NotificationMapper.MapBulkJobStatus(response), McpJsonDefaults.Options);
    }
}
