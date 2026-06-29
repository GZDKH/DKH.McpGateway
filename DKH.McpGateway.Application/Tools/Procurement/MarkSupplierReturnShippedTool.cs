using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class MarkSupplierReturnShippedTool
{
    [McpServerTool(Name = "mark_supplier_return_shipped"), Description(
        "Mark a supplier return order as shipped, recording the shipment timestamp.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Supplier return order ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var order = await client.MarkSupplierReturnShippedAsync(
            new MarkSupplierReturnShippedRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSupplierReturnOrderTool.MapSupplierReturn(order), McpJsonDefaults.Options);
    }
}
