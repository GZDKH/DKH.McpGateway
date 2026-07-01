using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class CreateProductRequestTool
{
    [McpServerTool(Name = "create_product_request"), Description(
        "Create a product request. customerId is an opaque ID; no email/phone/address is stored.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Storefront ID (GUID)")] string storefrontId,
        [Description("Opaque customer ID")] string customerId,
        [Description("Language code, e.g. en")] string languageCode,
        [Description("Requested product name")] string productName,
        [Description("Request description")] string description,
        [Description("Optional category")] string? category = null,
        [Description("Optional source URL")] string? sourceUrl = null,
        [Description("Optional desired price")] double? desiredPrice = null,
        [Description("Optional desired quantity")] int? desiredQuantity = null,
        [Description("Photo URLs as comma-separated values or JSON string array (optional)")] string? photoUrls = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(storefrontId))
        {
            return McpProtoHelper.FormatError("storefrontId is required");
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return McpProtoHelper.FormatError("customerId is required");
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

        var parsedPhotoUrls = ProductRequestInput.ParseStringList(photoUrls, nameof(photoUrls), out var error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var request = new CreateProductRequestRequest
        {
            StorefrontId = new GuidValue(storefrontId),
            CustomerId = customerId,
            LanguageCode = languageCode,
            ProductName = productName,
            Description = description,
            DesiredPrice = desiredPrice,
            DesiredQuantity = desiredQuantity,
        };

        if (!string.IsNullOrWhiteSpace(category))
        {
            request.Category = category;
        }

        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            request.SourceUrl = sourceUrl;
        }

        request.PhotoUrls.AddRange(parsedPhotoUrls);

        var model = await client.CreateAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ProductRequestMapper.MapProductRequest(model), McpJsonDefaults.Options);
    }
}
