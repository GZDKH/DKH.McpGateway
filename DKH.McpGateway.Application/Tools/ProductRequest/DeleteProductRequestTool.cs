using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class DeleteProductRequestTool
{
    [McpServerTool(Name = "delete_product_request"), Description("Soft-delete a product request.")]
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

        await client.DeleteAsync(new DeleteProductRequestRequest { Id = new GuidValue(id) }, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { deleted = true, id }, McpJsonDefaults.Options);
    }
}
