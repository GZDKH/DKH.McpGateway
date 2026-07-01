using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class PermanentlyDeleteProductRequestTool
{
    [McpServerTool(Name = "permanently_delete_product_request"), Description(
        "Permanently hard-delete a product request. This destructive operation cannot be undone.")]
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

        await client.PermanentlyDeleteAsync(
            new PermanentlyDeleteProductRequestRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { permanentlyDeleted = true, id }, McpJsonDefaults.Options);
    }
}
