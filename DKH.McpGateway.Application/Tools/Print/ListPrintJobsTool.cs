using DKH.PrintService.Contracts.Models.V1;
using DKH.PrintService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Print;

[McpServerToolType]
public static class ListPrintJobsTool
{
    [McpServerTool(Name = "list_print_jobs"), Description(
        "List print jobs, optionally filtered by lifecycle status " +
        "(Queued/Dispatched/Rendering/Sent/Confirmed/Failed), with pagination. Returns the jobs " +
        "plus total count and paging metadata.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Prints.PrintsClient client,
        [Description("Filter by job status (optional): Queued, Dispatched, Rendering, Sent, Confirmed, Failed")] string? status = null,
        [Description("Page number (1-based, default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListJobsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            request.Status = parsedStatus;
        }

        var response = await client.ListJobsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(
            new
            {
                items = response.Jobs.Select(GetPrintJobTool.MapJob),
                totalCount = response.Metadata.TotalCount,
                page = response.Metadata.CurrentPage,
                pageSize = response.Metadata.PageSize,
            },
            McpJsonDefaults.Options);
    }
}
