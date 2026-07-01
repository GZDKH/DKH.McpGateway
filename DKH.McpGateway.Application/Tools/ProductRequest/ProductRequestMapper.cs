using DKH.ProductRequestService.Contracts.ProductRequest.Models.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

internal static class ProductRequestMapper
{
    // No contact PII is present in ProductRequestModel. customerId is an opaque id and is retained.
    internal static object MapProductRequest(ProductRequestModel m) => new
    {
        id = m.Id?.Value,
        storefrontId = m.StorefrontId?.Value,
        customerId = m.CustomerId,
        status = m.Status.ToString(),
        languageCode = m.LanguageCode,
        productName = m.ProductName,
        description = m.Description,
        category = m.Category,
        sourceUrl = m.SourceUrl,
        desiredPrice = m.DesiredPrice,
        desiredQuantity = m.DesiredQuantity,
        photoUrls = m.PhotoUrls.ToArray(),
        adminNotes = m.AdminNotes,
        resolutionNote = m.ResolutionNote,
        foundProductId = m.FoundProductId?.Value,
        createdAt = m.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = m.UpdatedAt?.ToDateTimeOffset().ToString("O"),
        translations = m.Translations.Select(t => new
        {
            id = t.Id?.Value,
            languageCode = t.LanguageCode,
            productName = t.ProductName,
            description = t.Description,
            adminNotes = t.AdminNotes,
        }),
    };
}
