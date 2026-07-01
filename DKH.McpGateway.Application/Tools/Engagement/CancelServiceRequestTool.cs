using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class CancelServiceRequestTool
{
    [McpServerTool(Name = "cancel_service_request"), Description("Cancel an engagement service request.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        [Description("Service request ID")] string id,
        [Description("Cancellation reason (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(id, nameof(id), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CancelServiceRequestAsync(
            new CancelServiceRequestRequest { Id = EngagementToolInput.ToGuid(id), Reason = EngagementToolInput.OptionalString(reason) },
            cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapRequest(response));
    }
}
