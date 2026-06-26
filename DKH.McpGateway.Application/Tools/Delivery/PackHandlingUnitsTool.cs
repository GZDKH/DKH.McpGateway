using DKH.DeliveryService.Contracts.Delivery.Api.Dispatch.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class PackHandlingUnitsTool
{
    [McpServerTool(Name = "pack_handling_units"), Description(
        "Pack handling units (boxes, pallets, etc.) for a fulfillment. " +
        "Accepts a JSON array of unit items. Each item: { unitType, lengthCm, widthCm, heightCm, weightKg, " +
        "lines: [{ orderLineId, quantity }] }. " +
        "Returns the packed handling units with IDs.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DispatchService.DispatchServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("JSON array of handling unit items to pack")] string units,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(units))
        {
            return McpProtoHelper.FormatError("units is required");
        }

        List<PackHandlingUnitItemDto>? unitDtos;
        try
        {
            unitDtos = JsonSerializer.Deserialize<List<PackHandlingUnitItemDto>>(units, McpJsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            return McpProtoHelper.FormatError($"units JSON is invalid: {ex.Message}");
        }

        if (unitDtos is null || unitDtos.Count == 0)
        {
            return McpProtoHelper.FormatError("units must contain at least one item");
        }

        var request = new PackHandlingUnitsRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
        };

        foreach (var dto in unitDtos)
        {
            var item = new PackHandlingUnitItem
            {
                UnitType = Enum.Parse<HandlingUnitType>(dto.UnitType, ignoreCase: true),
                LengthCm = dto.LengthCm,
                WidthCm = dto.WidthCm,
                HeightCm = dto.HeightCm,
                WeightKg = dto.WeightKg,
            };

            foreach (var line in dto.Lines ?? [])
            {
                item.Lines.Add(new PackHandlingUnitLine
                {
                    OrderLineId = new GuidValue(line.OrderLineId),
                    Quantity = line.Quantity,
                });
            }

            request.Units.Add(item);
        }

        var response = await client.PackHandlingUnitsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            units = response.Units.Select(MapHandlingUnit),
        }, McpJsonDefaults.Options);
    }

    internal static object MapHandlingUnit(HandlingUnitModel h) => new
    {
        id = h.Id?.Value,
        fulfillmentId = h.FulfillmentId?.Value,
        unitType = h.UnitType.ToString(),
        lengthCm = h.LengthCm,
        widthCm = h.WidthCm,
        heightCm = h.HeightCm,
        weightKg = h.WeightKg,
        lines = h.Lines.Select(l => new
        {
            orderLineId = l.OrderLineId?.Value,
            quantity = l.Quantity,
        }),
        packedAt = h.PackedAt?.ToDateTimeOffset().ToString("O"),
    };

    private sealed record PackHandlingUnitLineDto(string OrderLineId, int Quantity);

    private sealed record PackHandlingUnitItemDto(
        string UnitType,
        double LengthCm,
        double WidthCm,
        double HeightCm,
        double WeightKg,
        List<PackHandlingUnitLineDto>? Lines);
}
