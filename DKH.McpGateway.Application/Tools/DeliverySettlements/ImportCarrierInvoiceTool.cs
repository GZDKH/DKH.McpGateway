using DKH.DeliveryService.Contracts.Delivery.Api.Settlements.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.DeliverySettlements;

[McpServerToolType]
public static class ImportCarrierInvoiceTool
{
    [McpServerTool(Name = "import_carrier_invoice"), Description(
        "Import a carrier invoice to create settlement lines for a fulfillment. " +
        "Accepts a JSON array of invoice lines. Each line: { type, amount, lineRef }. " +
        "Returns the created settlement record.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SettlementsService.SettlementsServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("Shipment ID (GUID)")] string shipmentId,
        [Description("Carrier party ID (GUID)")] string carrierPartyId,
        [Description("Currency code (e.g. USD)")] string currency,
        [Description("External invoice reference")] string externalRef,
        [Description("JSON array of invoice lines: [{ type, amount, lineRef }]")] string lines,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            return McpProtoHelper.FormatError("shipmentId is required");
        }

        if (string.IsNullOrWhiteSpace(carrierPartyId))
        {
            return McpProtoHelper.FormatError("carrierPartyId is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        if (string.IsNullOrWhiteSpace(externalRef))
        {
            return McpProtoHelper.FormatError("externalRef is required");
        }

        if (string.IsNullOrWhiteSpace(lines))
        {
            return McpProtoHelper.FormatError("lines is required");
        }

        List<InvoiceLineDto>? lineDtos;
        try
        {
            lineDtos = JsonSerializer.Deserialize<List<InvoiceLineDto>>(lines, McpJsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            return McpProtoHelper.FormatError($"lines JSON is invalid: {ex.Message}");
        }

        if (lineDtos is null || lineDtos.Count == 0)
        {
            return McpProtoHelper.FormatError("lines must contain at least one item");
        }

        var request = new ImportCarrierInvoiceRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
            ShipmentId = new GuidValue(shipmentId),
            CarrierPartyId = new GuidValue(carrierPartyId),
            Currency = currency,
            ExternalRef = externalRef,
        };

        foreach (var dto in lineDtos)
        {
            request.Lines.Add(new ImportCarrierInvoiceLine
            {
                Type = Enum.Parse<SettlementLineType>(dto.Type, ignoreCase: true),
                Amount = dto.Amount,
                LineRef = dto.LineRef,
            });
        }

        var settlement = await client.ImportCarrierInvoiceAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSettlementTool.MapSettlement(settlement), McpJsonDefaults.Options);
    }

    private sealed record InvoiceLineDto(string Type, double Amount, string LineRef);
}
