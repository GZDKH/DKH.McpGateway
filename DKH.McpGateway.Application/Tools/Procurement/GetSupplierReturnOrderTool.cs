using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetSupplierReturnOrderTool
{
    [McpServerTool(Name = "get_supplier_return_order"), Description(
        "Get a supplier return order by its ID. " +
        "Returns return status, lines, manifest details, and expected credit.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Supplier return order ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var order = await client.GetSupplierReturnOrderAsync(
            new GetSupplierReturnOrderRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapSupplierReturn(order), McpJsonDefaults.Options);
    }

    internal static object MapSupplierReturn(SupplierReturnOrderModel o) => new
    {
        id = o.Id?.Value,
        purchaseOrderId = o.PurchaseOrderId?.Value,
        supplierCounterpartyId = o.SupplierCounterpartyId?.Value,
        sourceWarehouseId = o.SourceWarehouseId?.Value,
        createdBy = o.CreatedBy?.Value,
        status = o.Status.ToString(),
        reason = o.Reason,
        createdAt = o.CreatedAt?.ToDateTimeOffset().ToString("O"),
        manifestNumber = o.ManifestNumber,
        manifestGeneratedAt = o.ManifestGeneratedAt?.ToDateTimeOffset().ToString("O"),
        shippedAt = o.ShippedAt?.ToDateTimeOffset().ToString("O"),
        expectedCreditAmount = o.ExpectedCreditAmount?.ToDecimal(),
        expectedCreditCurrency = o.ExpectedCreditCurrency,
        creditDueAt = o.CreditDueAt?.ToDateTimeOffset().ToString("O"),
        lines = o.Lines.Select(MapReturnLine),
    };

    internal static object MapReturnLine(SupplierReturnLineModel l) => new
    {
        catalogRef = l.CatalogRef?.Value,
        quantity = l.Quantity,
        reason = l.Reason,
    };
}
