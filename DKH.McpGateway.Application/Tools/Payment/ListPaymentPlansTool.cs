using DKH.PaymentService.Contracts.Models.V1;
using DKH.PaymentService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Payment;

[McpServerToolType]
public static class ListPaymentPlansTool
{
    [McpServerTool(Name = "list_payment_plans"), Description(
        "List payment plans with optional filtering. " +
        "Provide storefrontId to scope to a storefront. " +
        "Filter by status (Created, PartiallyCaptured, FullyCaptured, Cancelled) and orderId. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Payments.PaymentsClient client,
        [Description("Storefront ID (GUID, optional)")] string? storefrontId = null,
        [Description("Plan status filter: Created, PartiallyCaptured, FullyCaptured, Cancelled (optional)")] string? status = null,
        [Description("Order ID (GUID, optional)")] string? orderId = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListPaymentPlansRequest
        {
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
        };

        if (!string.IsNullOrWhiteSpace(storefrontId))
        {
            request.StorefrontId = new GuidValue(storefrontId);
        }

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            request.OrderId = new GuidValue(orderId);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<PaymentPlanStatus>(status, ignoreCase: true, out var planStatus))
        {
            request.Status = planStatus;
        }

        var response = await client.ListPaymentPlansAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(s => new
            {
                id = s.Id?.Value,
                storefrontId = s.StorefrontId?.Value,
                orderId = s.OrderId?.Value,
                totalAmount = s.TotalAmount,
                capturedAmount = s.CapturedAmount,
                currency = s.Currency,
                status = s.Status.ToString(),
                createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
                createdByUserId = s.CreatedByUserId?.Value,
            }),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
