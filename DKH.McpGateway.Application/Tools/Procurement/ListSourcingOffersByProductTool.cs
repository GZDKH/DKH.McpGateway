using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ListSourcingOffersByProductTool
{
    [McpServerTool(Name = "list_sourcing_offers_by_product"), Description(
        "List all sourcing offers for a specific product catalog reference. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Product catalog reference ID (GUID)")] string catalogRef,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(catalogRef))
        {
            return McpProtoHelper.FormatError("catalogRef is required");
        }

        var response = await client.ListSourcingOffersByProductAsync(
            new ListSourcingOffersByProductRequest
            {
                CatalogRef = new GuidValue(catalogRef),
                Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetSourcingOfferTool.MapSourcingOffer),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? page,
            pageSize = response.Metadata?.PageSize ?? pageSize,
        }, McpJsonDefaults.Options);
    }
}
