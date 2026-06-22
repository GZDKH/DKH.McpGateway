using DKH.PaymentService.Contracts.Models.V1;
using DKH.PaymentService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Payment;

[McpServerToolType]
public static class ListPaymentsTool
{
    [McpServerTool(Name = "list_payments"), Description(
        "List payments with optional filtering. " +
        "Provide storefrontId to scope to a storefront. " +
        "Filter by orderId, statuses (comma-separated: Initiated, Pending, Processing, Paid, Failed, Cancelled, Expired, PartiallyRefunded, Refunded), " +
        "and date range (ISO 8601). " +
        "Supports pagination via page and pageSize. " +
        "Customer PII (email, phone) is omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Payments.PaymentsClient client,
        [Description("Storefront ID (GUID, optional)")] string? storefrontId = null,
        [Description("Order ID (GUID, optional)")] string? orderId = null,
        [Description("Comma-separated payment statuses to filter by (optional)")] string? statuses = null,
        [Description("Start of date range (ISO 8601, optional)")] string? fromDate = null,
        [Description("End of date range (ISO 8601, optional)")] string? toDate = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListPaymentsRequest
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

        if (!string.IsNullOrWhiteSpace(statuses))
        {
            foreach (var raw in statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<PaymentStatus>(raw, ignoreCase: true, out var status))
                {
                    request.Statuses.Add(status);
                }
            }
        }

        // from_date / to_date are google.protobuf.StringValue → C# nullable string.
        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            request.FromDate = fromDate;
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            request.ToDate = toDate;
        }

        var response = await client.ListPaymentsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetPaymentTool.MapPayment),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
