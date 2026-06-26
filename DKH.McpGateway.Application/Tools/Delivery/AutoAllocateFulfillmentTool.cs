using DKH.DeliveryService.Contracts.Delivery.Api.Allocation.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class AutoAllocateFulfillmentTool
{
    [McpServerTool(Name = "auto_allocate_fulfillment"), Description(
        "Automatically allocate a fulfillment using the system's allocation engine. " +
        "Accepts a JSON array of demand items. Each item: { orderLineId, productId, variantId, quantity }. " +
        "Optionally restrict to candidate warehouse IDs (comma-separated GUIDs). " +
        "Returns the updated fulfillment (delivery address omitted).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AllocationService.AllocationServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("JSON array of demand items: [{ orderLineId, productId, variantId, quantity }]")] string demands,
        [Description("Comma-separated candidate warehouse IDs (GUIDs, optional)")] string? candidateWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(demands))
        {
            return McpProtoHelper.FormatError("demands is required");
        }

        List<DemandDto>? demandDtos;
        try
        {
            demandDtos = JsonSerializer.Deserialize<List<DemandDto>>(demands, McpJsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            return McpProtoHelper.FormatError($"demands JSON is invalid: {ex.Message}");
        }

        if (demandDtos is null || demandDtos.Count == 0)
        {
            return McpProtoHelper.FormatError("demands must contain at least one item");
        }

        var request = new AutoAllocateFulfillmentRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
        };

        foreach (var dto in demandDtos)
        {
            request.Demands.Add(new AutoAllocateFulfillmentDemand
            {
                OrderLineId = new GuidValue(dto.OrderLineId),
                ProductId = new GuidValue(dto.ProductId),
                VariantId = new GuidValue(dto.VariantId),
                Quantity = dto.Quantity,
            });
        }

        if (!string.IsNullOrWhiteSpace(candidateWarehouseIds))
        {
            foreach (var id in candidateWarehouseIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.CandidateWarehouseIds.Add(new GuidValue(id));
            }
        }

        var fulfillment = await client.AutoAllocateFulfillmentAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetFulfillmentTool.MapFulfillment(fulfillment), McpJsonDefaults.Options);
    }

    private sealed record DemandDto(string OrderLineId, string ProductId, string VariantId, int Quantity);
}
