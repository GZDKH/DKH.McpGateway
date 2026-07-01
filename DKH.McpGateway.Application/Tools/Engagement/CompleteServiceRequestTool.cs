using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class CompleteServiceRequestTool
{
    [McpServerTool(Name = "complete_service_request"), Description("Complete an engagement service request.")]
    public static async Task<string> ExecuteAsync(IApiKeyContext apiKeyContext, EngagementGrpc.EngagementServiceClient client, string id, CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(id, nameof(id), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CompleteServiceRequestAsync(new CompleteServiceRequestRequest { Id = EngagementToolInput.ToGuid(id) }, cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapRequest(response));
    }
}
