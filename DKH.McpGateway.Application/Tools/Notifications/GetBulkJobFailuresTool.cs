using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using NotificationGrpc = DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1.NotificationManagementService;

namespace DKH.McpGateway.Application.Tools.Notifications;

[McpServerToolType]
public static class GetBulkJobFailuresTool
{
    [McpServerTool(Name = "get_bulk_job_failures"), Description(
        "List failed recipients for a bulk notification job with recipient contact values masked.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NotificationGrpc.NotificationManagementServiceClient client,
        [Description("Bulk notification job ID (GUID)")] string jobId,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return McpProtoHelper.FormatError("jobId is required");
        }

        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? 20;
        var response = await client.GetBulkJobFailuresAsync(
            new GetBulkJobFailuresRequest
            {
                JobId = new GuidValue(jobId),
                Pagination = new PaginationRequest
                {
                    Page = resolvedPage,
                    PageSize = resolvedPageSize,
                },
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Failures.Select(NotificationMapper.MapBulkRecipientFailure),
            pagination = NotificationMapper.MapPagination(response.Pagination, resolvedPage, resolvedPageSize),
        }, McpJsonDefaults.Options);
    }
}
