using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetProductDemandTool
{
    [McpServerTool(Name = "get_product_demand"), Description(
        "Get aggregated product demand signals for a storefront. " +
        "Returns product request IDs and recorded timestamps. " +
        "Customer identity is intentionally omitted (PII).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Storefront ID (GUID)")] string storefrontId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(storefrontId))
        {
            return McpProtoHelper.FormatError("storefrontId is required");
        }

        var response = await client.GetProductDemandAsync(
            new GetProductDemandRequest { StorefrontId = new GuidValue(storefrontId) },
            cancellationToken: cancellationToken);

        // OMIT d.CustomerId (PII — DemandSignalEntryModel.customer_id)
        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(d => new
            {
                productRequestId = d.ProductRequestId?.Value,
                recordedAt = d.RecordedAt?.ToDateTimeOffset().ToString("O"),
            }),
        }, McpJsonDefaults.Options);
    }
}
