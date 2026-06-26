using DKH.DeliveryService.Contracts.Delivery.Api.DeliveryCrud.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class CreateDeliveryQuoteTool
{
    [McpServerTool(Name = "create_delivery_quote"), Description(
        "Create a delivery quote for a cart or order. " +
        "Requires a rate snapshot with carrier, service level, amount, and currency. " +
        "Returns the created quote with ID and status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeliveryCrudService.DeliveryCrudServiceClient client,
        [Description("Carrier counterparty ID (GUID)")] string carrierCounterpartyId,
        [Description("Service level (e.g. Standard, Express, Overnight)")] string serviceLevel,
        [Description("Rate amount as string (e.g. '150.00')")] string amount,
        [Description("Rate currency code (e.g. USD, EUR)")] string currency,
        [Description("Quote expiry timestamp (ISO 8601)")] string expiresAt,
        [Description("Cart ID (GUID, optional)")] string? cartId = null,
        [Description("Order ID (GUID, optional)")] string? orderId = null,
        [Description("Estimated transit hours (optional)")] int? estimatedTransitHours = null,
        [Description("Carrier provider reference string (optional)")] string? providerReference = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(carrierCounterpartyId))
        {
            return McpProtoHelper.FormatError("carrierCounterpartyId is required");
        }

        if (string.IsNullOrWhiteSpace(serviceLevel))
        {
            return McpProtoHelper.FormatError("serviceLevel is required");
        }

        if (string.IsNullOrWhiteSpace(amount))
        {
            return McpProtoHelper.FormatError("amount is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        if (string.IsNullOrWhiteSpace(expiresAt))
        {
            return McpProtoHelper.FormatError("expiresAt is required");
        }

        var rate = new QuoteRateSnapshot
        {
            CarrierCounterpartyId = new GuidValue(carrierCounterpartyId),
            ServiceLevel = Enum.Parse<QuoteServiceLevel>(serviceLevel, ignoreCase: true),
            Amount = amount,
            Currency = currency,
        };

        if (estimatedTransitHours.HasValue)
        {
            rate.EstimatedTransitHours = estimatedTransitHours.Value;
        }

        if (!string.IsNullOrWhiteSpace(providerReference))
        {
            rate.ProviderReference = providerReference;
        }

        var request = new CreateQuoteRequest { Rate = rate };

        if (DateTime.TryParse(expiresAt, out var expiresAtDate))
        {
            request.ExpiresAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(expiresAtDate.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(cartId))
        {
            request.CartId = new GuidValue(cartId);
        }

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            request.OrderId = new GuidValue(orderId);
        }

        var quote = await client.CreateQuoteAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapQuote(quote), McpJsonDefaults.Options);
    }

    internal static object MapQuote(QuoteModel q) => new
    {
        id = q.Id?.Value,
        tenantId = q.TenantId?.Value,
        cartId = q.CartId?.Value,
        orderId = q.OrderId?.Value,
        status = q.Status.ToString(),
        rate = q.Rate is null ? null : new
        {
            carrierCounterpartyId = q.Rate.CarrierCounterpartyId?.Value,
            serviceLevel = q.Rate.ServiceLevel.ToString(),
            amount = q.Rate.Amount,
            currency = q.Rate.Currency,
            estimatedTransitHours = q.Rate.EstimatedTransitHours,
            providerReference = q.Rate.ProviderReference,
        },
        expiresAt = q.ExpiresAt?.ToDateTimeOffset().ToString("O"),
        createdAt = q.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = q.UpdatedAt?.ToDateTimeOffset().ToString("O"),
        acceptedByUserId = q.AcceptedByUserId?.Value,
        acceptedAt = q.AcceptedAt?.ToDateTimeOffset().ToString("O"),
    };
}
