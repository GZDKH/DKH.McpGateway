using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class ListAssignedRequestsTool
{
    [McpServerTool(Name = "list_assigned_requests"), Description("List requests assigned to a provider. Provider lookup key is not echoed.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        [Description("Provider Keycloak user ID")] string providerKeycloakUserId,
        [Description("ServiceRequestStatus filter (optional)")] string? status = null,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!EngagementToolInput.Required(providerKeycloakUserId, nameof(providerKeycloakUserId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var request = new ListAssignedRequestsRequest
        {
            ProviderKeycloakUserId = EngagementToolInput.ToGuid(providerKeycloakUserId),
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 100),
            Status = EngagementToolInput.ParseOptionalEnum<ServiceRequestStatus>(status, out error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListAssignedRequestsAsync(request, cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, EngagementMapper.MapRequest));
    }
}
