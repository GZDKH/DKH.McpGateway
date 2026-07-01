using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using DKH.NotificationService.Contracts.Notification.Models.BulkJobStatus.v1;
using NotificationGrpc = DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1.NotificationManagementService;

namespace DKH.McpGateway.Application.Tools.Notifications;

[McpServerToolType]
public static class ListBulkJobsTool
{
    [McpServerTool(Name = "list_bulk_jobs"), Description(
        "List bulk notification jobs with optional status filter. Read-only; recipient PII is not returned.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NotificationGrpc.NotificationManagementServiceClient client,
        [Description("Filter by status: Pending, Processing, Completed, Cancelled, Failed (optional)")] string? status = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? 20;
        var request = new ListBulkJobsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = resolvedPage,
                PageSize = resolvedPageSize,
            },
        };

        var parsedStatus = NotificationInput.ParseBulkJobStatus(status);
        if (parsedStatus != BulkJobStatus.Unspecified)
        {
            request.Status = parsedStatus;
        }

        var response = await client.ListBulkJobsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Jobs.Select(NotificationMapper.MapBulkJobSummary),
            pagination = NotificationMapper.MapPagination(response.Pagination, resolvedPage, resolvedPageSize),
        }, McpJsonDefaults.Options);
    }
}
