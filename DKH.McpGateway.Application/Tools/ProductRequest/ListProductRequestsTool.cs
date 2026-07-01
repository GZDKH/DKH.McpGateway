using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class ListProductRequestsTool
{
    [McpServerTool(Name = "list_product_requests"), Description(
        "List product requests for a storefront with optional status/customer filters. " +
        "customerId is an opaque ID; no email/phone/address is stored.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Storefront ID (GUID)")] string storefrontId,
        [Description("Status filter: new | in_review | found | not_found | cancelled (optional)")] string? status = null,
        [Description("Opaque customer ID filter (optional)")] string? customerId = null,
        [Description("Soft-delete filter: active (default), deleted, all")] string? softDeleteFilter = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(storefrontId))
        {
            return McpProtoHelper.FormatError("storefrontId is required");
        }

        var request = new ListProductRequestsRequest
        {
            StorefrontId = new GuidValue(storefrontId),
            Status = ProductRequestInput.ParseStatus(status),
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
            SoftDeleteFilter = ProductRequestInput.ParseSoftDeleteFilter(softDeleteFilter),
        };

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            request.CustomerId = customerId;
        }

        var response = await client.ListAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(ProductRequestMapper.MapProductRequest),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
