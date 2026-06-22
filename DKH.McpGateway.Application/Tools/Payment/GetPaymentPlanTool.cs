using DKH.PaymentService.Contracts.Models.V1;
using DKH.PaymentService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Payment;

[McpServerToolType]
public static class GetPaymentPlanTool
{
    [McpServerTool(Name = "get_payment_plan"), Description(
        "Get a split payment plan by its ID. " +
        "Returns plan status, total and captured amounts, currency, and schedule entries with " +
        "due dates, captured amounts, and payment method per entry.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Payments.PaymentsClient client,
        [Description("Payment plan ID (GUID)")] string planId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(planId))
        {
            return McpProtoHelper.FormatError("planId is required");
        }

        var plan = await client.GetPaymentPlanAsync(
            new GetPaymentPlanRequest { PlanId = new GuidValue(planId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapPlan(plan), McpJsonDefaults.Options);
    }

    // StringValue fields (cancellation_reason, due_date, captured_amount, reference_text,
    // refund_reason) map to C# nullable string (no .Value); GuidValue fields use .Value.
    internal static object MapPlan(PaymentPlanModel p) => new
    {
        id = p.Id?.Value,
        storefrontId = p.StorefrontId?.Value,
        orderId = p.OrderId?.Value,
        totalAmount = p.TotalAmount,
        capturedAmount = p.CapturedAmount,
        remainingAmount = p.RemainingAmount,
        currency = p.Currency,
        status = p.Status.ToString(),
        cancellationReason = p.CancellationReason,
        cancelledAt = p.CancelledAt?.ToDateTimeOffset().ToString("O"),
        createdAt = p.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = p.UpdatedAt?.ToDateTimeOffset().ToString("O"),
        createdByUserId = p.CreatedByUserId?.Value,
        schedule = p.Schedule.Select(e => new
        {
            index = e.Index,
            type = e.Type.ToString(),
            dueAmount = e.DueAmount,
            dueDate = e.DueDate,
            status = e.Status.ToString(),
            capturedAmount = e.CapturedAmount,
            capturedAt = e.CapturedAt?.ToDateTimeOffset().ToString("O"),
            capturedByUserId = e.CapturedByUserId?.Value,
            method = e.Method.ToString(),
            referenceText = e.ReferenceText,
            refundReason = e.RefundReason,
            refundedByUserId = e.RefundedByUserId?.Value,
            refundMethod = e.RefundMethod.ToString(),
            refundedAt = e.RefundedAt?.ToDateTimeOffset().ToString("O"),
        }),
    };
}
