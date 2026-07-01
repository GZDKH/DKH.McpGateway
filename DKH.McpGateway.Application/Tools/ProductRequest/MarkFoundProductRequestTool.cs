using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class MarkFoundProductRequestTool
{
    [McpServerTool(Name = "mark_found_product_request"), Description("Mark a product request as found and link a catalog product.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Product request ID (GUID)")] string id,
        [Description("Found product ID (GUID)")] string productId,
        [Description("Optional transition note")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required");
        }

        var request = new MarkFoundRequest
        {
            Id = new GuidValue(id),
            ProductId = new GuidValue(productId),
        };

        if (!string.IsNullOrWhiteSpace(note))
        {
            request.Note = note;
        }

        var model = await client.MarkFoundAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ProductRequestMapper.MapProductRequest(model), McpJsonDefaults.Options);
    }
}
