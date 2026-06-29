using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class SignReceivingActTool
{
    [McpServerTool(Name = "sign_receiving_act"), Description(
        "Sign a receiving act for a purchase order, confirming goods were received at the warehouse. " +
        "Optionally provide accepted lines as a JSON array of {\"catalogRef\":\"<GUID>\",\"acceptedQuantity\":<int>} objects. " +
        "An empty acceptedLines array means accept all items in full.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Purchase order ID (GUID)")] string purchaseOrderId,
        [Description("User ID signing the act (GUID)")] string signedBy,
        [Description("Warehouse ID where goods were received (GUID)")] string warehouseId,
        [Description("Accepted lines as JSON array [{\"catalogRef\":\"...\",\"acceptedQuantity\":N}] (optional, empty=accept all)")] string? acceptedLines = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(purchaseOrderId))
        {
            return McpProtoHelper.FormatError("purchaseOrderId is required");
        }

        if (string.IsNullOrWhiteSpace(signedBy))
        {
            return McpProtoHelper.FormatError("signedBy is required");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required");
        }

        var request = new SignReceivingActRequest
        {
            PurchaseOrderId = new GuidValue(purchaseOrderId),
            SignedBy = new GuidValue(signedBy),
            WarehouseId = new GuidValue(warehouseId),
        };

        if (!string.IsNullOrWhiteSpace(acceptedLines))
        {
            try
            {
                var lines = JsonSerializer.Deserialize<List<JsonElement>>(acceptedLines, McpJsonDefaults.Options);
                if (lines is not null)
                {
                    foreach (var line in lines)
                    {
                        var catalogRef = line.GetProperty("catalogRef").GetString() ?? string.Empty;
                        var qty = line.GetProperty("acceptedQuantity").GetInt32();
                        request.AcceptedLines.Add(new AcceptedReceivingLine
                        {
                            CatalogRef = new GuidValue(catalogRef),
                            AcceptedQuantity = qty,
                        });
                    }
                }
            }
            catch (Exception)
            {
                return McpProtoHelper.FormatError("acceptedLines JSON is invalid");
            }
        }

        var act = await client.SignReceivingActAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetReceivingActTool.MapReceivingAct(act), McpJsonDefaults.Options);
    }
}
