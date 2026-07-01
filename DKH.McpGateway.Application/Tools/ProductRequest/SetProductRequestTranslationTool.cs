using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class SetProductRequestTranslationTool
{
    [McpServerTool(Name = "set_product_request_translation"), Description("Set localized product request fields.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Product request ID (GUID)")] string id,
        [Description("Language code, e.g. en")] string languageCode,
        [Description("Localized product name")] string productName,
        [Description("Localized description")] string description,
        [Description("Optional localized admin notes")] string? adminNotes = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return McpProtoHelper.FormatError("languageCode is required");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            return McpProtoHelper.FormatError("productName is required");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return McpProtoHelper.FormatError("description is required");
        }

        var request = new SetTranslationRequest
        {
            Id = new GuidValue(id),
            LanguageCode = languageCode,
            ProductName = productName,
            Description = description,
        };

        if (!string.IsNullOrWhiteSpace(adminNotes))
        {
            request.AdminNotes = adminNotes;
        }

        var model = await client.SetTranslationAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ProductRequestMapper.MapProductRequest(model), McpJsonDefaults.Options);
    }
}
