using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class AssignProviderTool
{
    [McpServerTool(Name = "assign_provider"), Description("Assign a provider to a service request. Provider identity values are not echoed.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        [Description("Service request ID")] string id,
        [Description("Provider Keycloak user ID")] string providerKeycloakUserId,
        [Description("ProviderSource")] string providerSource,
        [Description("Provider source ID (optional)")] string? providerSourceId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!EngagementToolInput.Required(id, nameof(id), out var error) ||
            !EngagementToolInput.Required(providerKeycloakUserId, nameof(providerKeycloakUserId), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var parsedSource = EngagementToolInput.ParseRequiredEnum<ProviderSource>(providerSource, nameof(providerSource), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.AssignProviderAsync(
            new AssignProviderRequest
            {
                Id = EngagementToolInput.ToGuid(id),
                Provider = new ServiceProviderModel
                {
                    KeycloakUserId = EngagementToolInput.ToGuid(providerKeycloakUserId),
                    Source = parsedSource,
                    SourceId = EngagementToolInput.ToGuidOrNull(providerSourceId),
                },
            },
            cancellationToken: cancellationToken);

        return EngagementToolOutput.Json(EngagementMapper.MapRequest(response));
    }
}
