using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

[McpServerToolType]
public static class UpdateProductRequestTool
{
    [McpServerTool(Name = "update_product_request"), Description(
        "Update product request details. Returns no contact PII; customerId is an opaque ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductRequestCrudService.ProductRequestCrudServiceClient client,
        [Description("Product request ID (GUID)")] string id,
        [Description("Requested product name")] string productName,
        [Description("Request description")] string description,
        [Description("Optional category")] string? category = null,
        [Description("Optional source URL")] string? sourceUrl = null,
        [Description("Optional admin notes")] string? adminNotes = null,
        [Description("Optional desired price")] double? desiredPrice = null,
        [Description("Optional desired quantity")] int? desiredQuantity = null,
        [Description("Photo URLs as comma-separated values or JSON string array (optional)")] string? photoUrls = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
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

        var request = new UpdateProductRequestRequest
        {
            Id = new GuidValue(id),
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

        if (!string.IsNullOrWhiteSpace(adminNotes))
        {
            request.AdminNotes = adminNotes;
        }

        request.PhotoUrls.AddRange(parsedPhotoUrls);

        var model = await client.UpdateAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ProductRequestMapper.MapProductRequest(model), McpJsonDefaults.Options);
    }
}
