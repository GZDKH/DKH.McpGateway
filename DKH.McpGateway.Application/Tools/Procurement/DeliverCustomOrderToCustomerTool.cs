using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class DeliverCustomOrderToCustomerTool
{
    [McpServerTool(Name = "deliver_custom_order_to_customer"), Description(
        "Mark a custom customer order as delivered to the customer. " +
        "This is a state transition only — balance capture is performed in the payment layer.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Custom customer order ID (GUID)")] string orderId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(orderId))
        {
            return McpProtoHelper.FormatError("orderId is required");
        }

        var order = await client.DeliverCustomOrderToCustomerAsync(
            new DeliverCustomOrderToCustomerRequest { OrderId = new GuidValue(orderId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCustomCustomerOrderTool.MapCustomOrder(order), McpJsonDefaults.Options);
    }
}
