using DKH.CartService.Contracts.Cart.Api.CartClaim.v1;

namespace DKH.McpGateway.Application.Tools.Cart;

[McpServerToolType]
public static class IssueClaimCodeTool
{
    [McpServerTool(Name = "issue_cart_claim_code"), Description(
        "Issue an HMAC-signed claim code for a cart to enable phone-to-POS handoff. " +
        "The customer shows the QR code or 4-letter code at the till; the cashier scans or types it to claim the cart. " +
        "Idempotent: re-issuing for the same cart invalidates the previous code. " +
        "Default TTL is 15 minutes; supply expiresAt to override.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CartClaimService.CartClaimServiceClient client,
        [Description("Cart ID (GUID) to issue the claim code for")] string cartId,
        [Description("Custom expiry time (ISO 8601, optional — defaults to 15 minutes from now)")] string? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(cartId))
        {
            return McpProtoHelper.FormatError("cartId is required");
        }

        var request = new IssueClaimCodeRequest
        {
            CartId = new GuidValue(cartId),
        };

        if (!string.IsNullOrWhiteSpace(expiresAt) && DateTime.TryParse(expiresAt, out var expiryDate))
        {
            request.ExpiresAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(expiryDate.ToUniversalTime());
        }

        var response = await client.IssueClaimCodeAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            code = response.ClaimCode?.Code,
            cartId = response.ClaimCode?.CartId?.Value,
            qrUrl = response.ClaimCode?.QrUrl,
            status = response.ClaimCode?.Status.ToString(),
            issuedAt = response.ClaimCode?.IssuedAt?.ToDateTimeOffset().ToString("O"),
            expiresAt = response.ClaimCode?.ExpiresAt?.ToDateTimeOffset().ToString("O"),
        }, McpJsonDefaults.Options);
    }
}
