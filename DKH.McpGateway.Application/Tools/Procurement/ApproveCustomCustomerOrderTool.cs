using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ApproveCustomCustomerOrderTool
{
    [McpServerTool(Name = "approve_custom_customer_order"), Description(
        "Approve a custom customer order pending approval. " +
        "Creates the underlying purchase order and returns it.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Custom customer order ID (GUID)")] string orderId,
        [Description("Manager user ID approving this order (GUID)")] string managerUserId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(orderId))
        {
            return McpProtoHelper.FormatError("orderId is required");
        }

        if (string.IsNullOrWhiteSpace(managerUserId))
        {
            return McpProtoHelper.FormatError("managerUserId is required");
        }

        var po = await client.ApproveCustomCustomerOrderAsync(
            new ApproveCustomCustomerOrderRequest
            {
                OrderId = new GuidValue(orderId),
                ManagerUserId = new GuidValue(managerUserId),
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetPurchaseOrderTool.MapPurchaseOrder(po), McpJsonDefaults.Options);
    }
}
