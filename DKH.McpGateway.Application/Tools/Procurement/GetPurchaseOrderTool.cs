using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetPurchaseOrderTool
{
    [McpServerTool(Name = "get_purchase_order"), Description(
        "Get a single purchase order by its ID. " +
        "Returns the PO header (status, counterparty, currency, delivery dates, item count) " +
        "plus all line items with quantities and pricing.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Purchase order ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var po = await client.GetPurchaseOrderAsync(
            new GetPurchaseOrderRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapPurchaseOrder(po), McpJsonDefaults.Options);
    }

    internal static object MapPurchaseOrder(PurchaseOrderModel po) => new
    {
        id = po.Id?.Value,
        subtype = po.Subtype.ToString(),
        status = po.Status.ToString(),
        counterpartyId = po.CounterpartyId?.Value,
        currency = po.Currency,
        plannedDeliveryAt = po.PlannedDeliveryAt?.ToDateTimeOffset().ToString("O"),
        actualDeliveryAt = po.ActualDeliveryAt?.ToDateTimeOffset().ToString("O"),
        unitCodeMode = po.UnitCodeMode.ToString(),
        boxCodeMode = po.BoxCodeMode.ToString(),
        requiresInspection = po.RequiresInspection,
        createdAt = po.CreatedAt?.ToDateTimeOffset().ToString("O"),
        createdBy = po.CreatedBy?.Value,
        itemCount = po.ItemCount,
        customWorkflowStatus = po.CustomWorkflowStatus.ToString(),
        items = po.Items.Select(MapPoItem),
    };

    internal static object MapPoItem(PurchaseOrderItemModel item) => new
    {
        id = item.Id?.Value,
        catalogRef = item.CatalogRef?.Value,
        materialRef = item.MaterialRef?.Value,
        quantity = item.Quantity,
        unitPriceAmount = item.UnitPriceAmount,
        unitPriceCurrency = item.UnitPriceCurrency,
        unitPriceAmountDecimal = item.UnitPriceAmountDecimal?.ToDecimal(),
    };
}
