using DKH.DeliveryService.Contracts.Delivery.Api.Claims.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.DeliveryClaims;

[McpServerToolType]
public static class UpdateClaimStatusTool
{
    [McpServerTool(Name = "update_claim_status"), Description(
        "Transition the status of an existing delivery claim. " +
        "Returns the updated claim.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ClaimsService.ClaimsServiceClient client,
        [Description("Claim ID (GUID)")] string claimId,
        [Description("Target status (e.g. UnderReview, Approved, Rejected, Settled)")] string nextStatus,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(claimId))
        {
            return McpProtoHelper.FormatError("claimId is required");
        }

        if (string.IsNullOrWhiteSpace(nextStatus))
        {
            return McpProtoHelper.FormatError("nextStatus is required");
        }

        var request = new UpdateClaimStatusRequest
        {
            ClaimId = new GuidValue(claimId),
            NextStatus = Enum.Parse<ClaimStatus>(nextStatus, ignoreCase: true),
        };

        var claim = await client.UpdateClaimStatusAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(OpenDeliveryClaimTool.MapClaim(claim), McpJsonDefaults.Options);
    }
}
