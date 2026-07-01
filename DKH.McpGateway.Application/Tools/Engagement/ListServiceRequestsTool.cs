using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class ListServiceRequestsTool
{
    [McpServerTool(Name = "list_service_requests"), Description("List engagement service requests with requester/provider identity values omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        [Description("ServiceRequestStatus filter (optional)")] string? status = null,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var request = new ListServiceRequestsRequest
        {
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 100),
            Status = EngagementToolInput.ParseOptionalEnum<ServiceRequestStatus>(status, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListServiceRequestsAsync(request, cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, EngagementMapper.MapRequest));
    }
}
