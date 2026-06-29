using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class SendCustomOrderToFactoryTool
{
    [McpServerTool(Name = "send_custom_order_to_factory"), Description(
        "Send an approved custom customer order to the factory for production. " +
        "Links the order to a factory production order ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Custom customer order ID (GUID)")] string orderId,
        [Description("Factory production order ID (GUID)")] string factoryProductionOrderId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(orderId))
        {
            return McpProtoHelper.FormatError("orderId is required");
        }

        if (string.IsNullOrWhiteSpace(factoryProductionOrderId))
        {
            return McpProtoHelper.FormatError("factoryProductionOrderId is required");
        }

        var order = await client.SendCustomOrderToFactoryAsync(
            new SendCustomOrderToFactoryRequest
            {
                OrderId = new GuidValue(orderId),
                FactoryProductionOrderId = new GuidValue(factoryProductionOrderId),
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCustomCustomerOrderTool.MapCustomOrder(order), McpJsonDefaults.Options);
    }
}
