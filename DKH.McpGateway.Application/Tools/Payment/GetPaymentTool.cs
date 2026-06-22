using DKH.PaymentService.Contracts.Models.V1;
using DKH.PaymentService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Payment;

[McpServerToolType]
public static class GetPaymentTool
{
    [McpServerTool(Name = "get_payment"), Description(
        "Get a single payment by its ID. " +
        "Returns payment status, amounts, currency, provider, order reference, timestamps, " +
        "refunds, and status history. " +
        "Customer PII (email, phone) is omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Payments.PaymentsClient client,
        [Description("Payment ID (GUID)")] string paymentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return McpProtoHelper.FormatError("paymentId is required");
        }

        var payment = await client.GetPaymentAsync(
            new GetPaymentRequest { Id = new GuidValue(paymentId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapPayment(payment), McpJsonDefaults.Options);
    }

    // PaymentModel: google.protobuf.StringValue fields map to C# nullable string (no .Value);
    // GuidValue fields are wrapper objects (.Value). customer_email/phone deliberately omitted (PII).
    internal static object MapPayment(PaymentModel p) => new
    {
        id = p.Id?.Value,
        paymentNumber = p.PaymentNumber,
        storefrontId = p.StorefrontId?.Value,
        orderId = p.OrderId?.Value,
        providerId = p.ProviderId,
        providerPaymentId = p.ProviderPaymentId,
        providerConfigId = p.ProviderConfigId?.Value,
        channel = p.Channel.ToString(),
        status = p.Status.ToString(),
        amount = p.Amount,
        currency = p.Currency,
        description = p.Description,
        paidAmount = p.PaidAmount,
        paidCurrency = p.PaidCurrency,
        paidAt = p.PaidAt?.ToDateTimeOffset().ToString("O"),
        cancelledAt = p.CancelledAt?.ToDateTimeOffset().ToString("O"),
        cancellationReason = p.CancellationReason,
        expiresAt = p.ExpiresAt?.ToDateTimeOffset().ToString("O"),
        createdAt = p.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = p.UpdatedAt?.ToDateTimeOffset().ToString("O"),
        refunds = p.Refunds.Select(r => new
        {
            id = r.Id?.Value,
            amount = r.Amount,
            currency = r.Currency,
            status = r.Status.ToString(),
            reason = r.Reason,
            initiatedBy = r.InitiatedBy,
            createdAt = r.CreatedAt?.ToDateTimeOffset().ToString("O"),
            completedAt = r.CompletedAt?.ToDateTimeOffset().ToString("O"),
        }),
        statusHistory = p.StatusHistory.Select(h => new
        {
            status = h.Status.ToString(),
            occurredAt = h.OccurredAt?.ToDateTimeOffset().ToString("O"),
            changedBy = h.ChangedBy,
            reason = h.Reason,
        }),
    };
}
