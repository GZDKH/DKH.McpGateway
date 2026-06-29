using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class CreateSupplierReturnOrderTool
{
    [McpServerTool(Name = "create_supplier_return_order"), Description(
        "Create a new supplier return order against a purchase order. " +
        "Lines is a JSON array of {\"catalogRef\":\"<GUID>\",\"quantity\":<int>,\"reason\":\"...\"} objects.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Purchase order ID (GUID)")] string purchaseOrderId,
        [Description("Supplier counterparty ID (GUID)")] string supplierCounterpartyId,
        [Description("Source warehouse ID (GUID)")] string sourceWarehouseId,
        [Description("User creating this return (GUID)")] string createdBy,
        [Description("Return lines as JSON array [{\"catalogRef\":\"...\",\"quantity\":N,\"reason\":\"...\"}]")] string lines,
        [Description("Overall return reason (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(purchaseOrderId))
        {
            return McpProtoHelper.FormatError("purchaseOrderId is required");
        }

        if (string.IsNullOrWhiteSpace(supplierCounterpartyId))
        {
            return McpProtoHelper.FormatError("supplierCounterpartyId is required");
        }

        if (string.IsNullOrWhiteSpace(sourceWarehouseId))
        {
            return McpProtoHelper.FormatError("sourceWarehouseId is required");
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return McpProtoHelper.FormatError("createdBy is required");
        }

        if (string.IsNullOrWhiteSpace(lines))
        {
            return McpProtoHelper.FormatError("lines is required");
        }

        var request = new CreateSupplierReturnOrderRequest
        {
            PurchaseOrderId = new GuidValue(purchaseOrderId),
            SupplierCounterpartyId = new GuidValue(supplierCounterpartyId),
            SourceWarehouseId = new GuidValue(sourceWarehouseId),
            CreatedBy = new GuidValue(createdBy),
            Reason = reason ?? string.Empty,
        };

        try
        {
            var lineItems = JsonSerializer.Deserialize<List<JsonElement>>(lines, McpJsonDefaults.Options);
            if (lineItems is not null)
            {
                foreach (var line in lineItems)
                {
                    var catalogRef = line.GetProperty("catalogRef").GetString() ?? string.Empty;
                    var qty = line.GetProperty("quantity").GetInt32();
                    var lineReason = line.TryGetProperty("reason", out var r) ? r.GetString() ?? string.Empty : string.Empty;
                    request.Lines.Add(new SupplierReturnLineRequest
                    {
                        CatalogRef = new GuidValue(catalogRef),
                        Quantity = qty,
                        Reason = lineReason,
                    });
                }
            }
        }
        catch (Exception)
        {
            return McpProtoHelper.FormatError("lines JSON is invalid");
        }

        var order = await client.CreateSupplierReturnOrderAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSupplierReturnOrderTool.MapSupplierReturn(order), McpJsonDefaults.Options);
    }
}
