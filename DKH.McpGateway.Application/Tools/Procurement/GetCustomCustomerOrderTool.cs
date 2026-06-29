using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetCustomCustomerOrderTool
{
    [McpServerTool(Name = "get_custom_customer_order"), Description(
        "Get a custom customer order by its ID. " +
        "Returns the underlying purchase order, modifications, pricing, and factory linkage. " +
        "Customer identity and delivery address are intentionally omitted (PII).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Custom customer order ID (GUID)")] string orderId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(orderId))
        {
            return McpProtoHelper.FormatError("orderId is required");
        }

        var order = await client.GetCustomCustomerOrderAsync(
            new GetCustomCustomerOrderRequest { OrderId = new GuidValue(orderId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapCustomOrder(order), McpJsonDefaults.Options);
    }

    internal static object MapCustomOrder(CustomCustomerOrderModel o) => new
    {
        // OMIT o.CustomerId (PII — CustomCustomerOrderModel.customer_id)
        // OMIT o.DeliveryAddress (PII — CustomCustomerOrderModel.delivery_address)
        order = o.Order is not null ? GetPurchaseOrderTool.MapPurchaseOrder(o.Order) : null,
        baseProductCatalogRef = o.BaseProductCatalogRef,
        modifications = o.Modifications.Select(m => new
        {
            field = m.Field,
            oldValue = m.OldValue,
            newValue = m.NewValue,
            notes = m.Notes,
        }),
        estimatedDeliveryDate = o.EstimatedDeliveryDate?.ToDateTimeOffset().ToString("O"),
        estimatedPrice = o.EstimatedPrice?.ToDecimal(),
        depositAmount = o.DepositAmount?.ToDecimal(),
        balanceAmount = o.BalanceAmount?.ToDecimal(),
        linkedFactoryProductionOrderId = o.LinkedFactoryProductionOrderId?.Value,
    };
}
