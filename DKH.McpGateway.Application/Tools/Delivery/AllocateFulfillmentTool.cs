using DKH.DeliveryService.Contracts.Delivery.Api.Allocation.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class AllocateFulfillmentTool
{
    [McpServerTool(Name = "allocate_fulfillment"), Description(
        "Manually allocate fulfillment lines to warehouses. " +
        "Accepts a JSON array of allocation lines. Each line: { orderLineId, productId, variantId, warehouseId, quantity, originCountryCode? }. " +
        "Returns the updated fulfillment (delivery address omitted).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AllocationService.AllocationServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("JSON array of allocation lines")] string lines,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(lines))
        {
            return McpProtoHelper.FormatError("lines is required");
        }

        List<AllocationLineDto>? lineDtos;
        try
        {
            lineDtos = JsonSerializer.Deserialize<List<AllocationLineDto>>(lines, McpJsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            return McpProtoHelper.FormatError($"lines JSON is invalid: {ex.Message}");
        }

        if (lineDtos is null || lineDtos.Count == 0)
        {
            return McpProtoHelper.FormatError("lines must contain at least one item");
        }

        var request = new AllocateFulfillmentRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
        };

        foreach (var dto in lineDtos)
        {
            request.Lines.Add(new AllocateFulfillmentLine
            {
                OrderLineId = new GuidValue(dto.OrderLineId),
                ProductId = new GuidValue(dto.ProductId),
                VariantId = new GuidValue(dto.VariantId),
                WarehouseId = new GuidValue(dto.WarehouseId),
                Quantity = dto.Quantity,
                OriginCountryCode = dto.OriginCountryCode ?? string.Empty,
            });
        }

        var fulfillment = await client.AllocateFulfillmentAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetFulfillmentTool.MapFulfillment(fulfillment), McpJsonDefaults.Options);
    }

    private sealed record AllocationLineDto(
        string OrderLineId,
        string ProductId,
        string VariantId,
        string WarehouseId,
        int Quantity,
        string? OriginCountryCode);
}
