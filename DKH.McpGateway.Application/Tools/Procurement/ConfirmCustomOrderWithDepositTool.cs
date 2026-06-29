using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ConfirmCustomOrderWithDepositTool
{
    [McpServerTool(Name = "confirm_custom_order_with_deposit"), Description(
        "Confirm a custom customer order after deposit has been captured in the payment layer. " +
        "This is a state transition only — no money movement is performed here.")]
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

        var order = await client.ConfirmCustomOrderWithDepositAsync(
            new ConfirmCustomOrderWithDepositRequest { OrderId = new GuidValue(orderId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCustomCustomerOrderTool.MapCustomOrder(order), McpJsonDefaults.Options);
    }
}
