using DKH.DeliveryService.Contracts.Delivery.Api.DeliveryCrud.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class GetFulfillmentTool
{
    [McpServerTool(Name = "get_fulfillment"), Description(
        "Get a single fulfillment by its ID. " +
        "Returns fulfillment details including allocation plan and declaration IDs. " +
        "Delivery address is omitted (PII).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeliveryCrudService.DeliveryCrudServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        var fulfillment = await client.GetFulfillmentAsync(
            new GetFulfillmentRequest { Id = new GuidValue(fulfillmentId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapFulfillment(fulfillment), McpJsonDefaults.Options);
    }

    /// <summary>
    /// Maps a FulfillmentModel to a safe anonymous object, omitting PII (delivery_address).
    /// Reused by Allocation, Cancellation, and CustomsLink tools.
    /// </summary>
    internal static object MapFulfillment(FulfillmentModel f) => new
    {
        id = f.Id?.Value,
        tenantId = f.TenantId?.Value,
        orderId = f.OrderId?.Value,
        quoteId = f.QuoteId?.Value,
        status = f.Status.ToString(),
        // deliveryAddress OMITTED (PII)
        warehouseId = f.WarehouseId?.Value,
        warehouseOperatorCounterpartyId = f.WarehouseOperatorCounterpartyId?.Value,
        cancellationReason = f.CancellationReason,
        createdAt = f.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = f.UpdatedAt?.ToDateTimeOffset().ToString("O"),
        dispatchedAt = f.DispatchedAt?.ToDateTimeOffset().ToString("O"),
        deliveredAt = f.DeliveredAt?.ToDateTimeOffset().ToString("O"),
        allocationPlan = f.AllocationPlan is null ? null : new
        {
            lines = f.AllocationPlan.Lines.Select(l => new
            {
                orderLineId = l.OrderLineId?.Value,
                productId = l.ProductId?.Value,
                variantId = l.VariantId?.Value,
                warehouseId = l.WarehouseId?.Value,
                quantity = l.Quantity,
                reservationId = l.ReservationId?.Value,
            }),
        },
        declarationIds = f.DeclarationIds.Select(d => d.Value),
    };
}
