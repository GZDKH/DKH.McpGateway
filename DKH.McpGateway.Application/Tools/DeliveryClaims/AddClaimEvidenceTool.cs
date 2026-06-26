using DKH.DeliveryService.Contracts.Delivery.Api.Claims.v1;

namespace DKH.McpGateway.Application.Tools.DeliveryClaims;

[McpServerToolType]
public static class AddClaimEvidenceTool
{
    [McpServerTool(Name = "add_claim_evidence"), Description(
        "Add evidence (document reference) to an existing delivery claim. " +
        "Returns the updated claim.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ClaimsService.ClaimsServiceClient client,
        [Description("Claim ID (GUID)")] string claimId,
        [Description("Document reference string")] string documentRef,
        [Description("Description of the evidence")] string description,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(claimId))
        {
            return McpProtoHelper.FormatError("claimId is required");
        }

        if (string.IsNullOrWhiteSpace(documentRef))
        {
            return McpProtoHelper.FormatError("documentRef is required");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return McpProtoHelper.FormatError("description is required");
        }

        var request = new AddEvidenceRequest
        {
            ClaimId = new GuidValue(claimId),
            DocumentRef = documentRef,
            Description = description,
        };

        var claim = await client.AddEvidenceAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(OpenDeliveryClaimTool.MapClaim(claim), McpJsonDefaults.Options);
    }
}
