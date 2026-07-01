using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class MarkNotFoundProductRequestTool
{
    [McpServerTool(Name = "mark_not_found_product_request"), Description("Mark a product request as not found.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Product request ID (GUID)")] string id,
        [Description("Optional transition note")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new MarkNotFoundRequest { Id = new GuidValue(id) };

        if (!string.IsNullOrWhiteSpace(note))
        {
            request.Note = note;
        }

        var model = await client.MarkNotFoundAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ProductRequestMapper.MapProductRequest(model), McpJsonDefaults.Options);
    }
}
