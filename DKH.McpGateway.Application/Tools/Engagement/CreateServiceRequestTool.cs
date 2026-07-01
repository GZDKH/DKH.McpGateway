using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class CreateServiceRequestTool
{
    [McpServerTool(Name = "create_service_request"), Description("Create an engagement service request. Requester identity values are not echoed.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        [Description("ServiceType")] string serviceType,
        [Description("Requester Keycloak user ID")] string requesterKeycloakUserId,
        [Description("RequesterSource")] string requesterSource,
        [Description("Price amount as decimal string")] string priceAmount,
        [Description("Price currency")] string priceCurrency,
        [Description("Requester source ID (optional)")] string? requesterSourceId = null,
        [Description("Product reference subject ID (optional; mutually exclusive with freeform subject)")] string? productRef = null,
        [Description("Freeform subject description (optional)")] string? freeformDescription = null,
        [Description("Freeform subject location (optional)")] string? freeformLocation = null,
        [Description("Deadline ISO-8601 timestamp (optional)")] string? deadline = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!EngagementToolInput.Required(requesterKeycloakUserId, nameof(requesterKeycloakUserId), out var error) ||
            !EngagementToolInput.Required(priceAmount, nameof(priceAmount), out error) ||
            !EngagementToolInput.Required(priceCurrency, nameof(priceCurrency), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var parsedServiceType = EngagementToolInput.ParseRequiredEnum<ServiceType>(serviceType, nameof(serviceType), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var parsedRequesterSource = EngagementToolInput.ParseRequiredEnum<RequesterSource>(requesterSource, nameof(requesterSource), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CreateServiceRequestAsync(
            new CreateServiceRequestRequest
            {
                ServiceType = parsedServiceType,
                Requester = new ServiceRequesterModel
                {
                    KeycloakUserId = EngagementToolInput.ToGuid(requesterKeycloakUserId),
                    Source = parsedRequesterSource,
                    SourceId = EngagementToolInput.ToGuidOrNull(requesterSourceId),
                },
                Subject = BuildSubject(productRef, freeformDescription, freeformLocation),
                Price = new MoneyModel { Amount = priceAmount.Trim(), Currency = priceCurrency.Trim() },
                Deadline = EngagementToolInput.ParseTimestamp(deadline),
            },
            cancellationToken: cancellationToken);

        return EngagementToolOutput.Json(EngagementMapper.MapRequest(response));
    }

    private static EngagementSubjectModel BuildSubject(string? productRef, string? freeformDescription, string? freeformLocation)
        => string.IsNullOrWhiteSpace(productRef)
            ? new EngagementSubjectModel
            {
                Freeform = new FreeformSubject
                {
                    Description = EngagementToolInput.OptionalString(freeformDescription) ?? string.Empty,
                    Location = EngagementToolInput.OptionalString(freeformLocation) ?? string.Empty,
                },
            }
            : new EngagementSubjectModel { ProductRef = EngagementToolInput.ToGuid(productRef) };
}
