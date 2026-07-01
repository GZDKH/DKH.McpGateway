using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class StartReviewProductRequestTool
{
    [McpServerTool(Name = "start_review_product_request"), Description("Move a product request into review.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Product request ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var model = await client.StartReviewAsync(
            new StartReviewRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ProductRequestMapper.MapProductRequest(model), McpJsonDefaults.Options);
    }
}
