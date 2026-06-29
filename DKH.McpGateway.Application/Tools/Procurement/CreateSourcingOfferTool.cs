using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class CreateSourcingOfferTool
{
    [McpServerTool(Name = "create_sourcing_offer"), Description(
        "Create a new sourcing offer for a product. " +
        "priceBreaks: JSON array [{\"minQuantity\":N,\"unitPriceAmount\":D,\"unitPriceCurrency\":\"...\"}]. " +
        "costComponents: JSON array [{\"kind\":\"...\",\"amount\":D,\"currency\":\"...\"}]. " +
        "participants: JSON array [{\"counterpartyId\":\"...\",\"role\":\"...\"}]. " +
        "certificates: JSON array [{\"certificateType\":\"...\",\"number\":\"...\",\"issuerCounterpartyId\":\"...\",\"validFrom\":\"...\",\"validTo\":\"...\",\"documentRef\":\"...\"}].")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Product catalog reference ID (GUID)")] string catalogRef,
        [Description("Seller counterparty ID (GUID)")] string sellerId,
        [Description("Origin country")] string originCountry,
        [Description("Origin region (optional)")] string? originRegion = null,
        [Description("Origin plantation (optional)")] string? originPlantation = null,
        [Description("Origin grade (optional)")] string? originGrade = null,
        [Description("Incoterms (optional)")] string? incoterms = null,
        [Description("Origin harvest year, 0 = unspecified (optional)")] int originHarvestYear = 0,
        [Description("Minimum order quantity, 0 = unspecified (optional)")] int minOrderQuantity = 0,
        [Description("Lead time in days, 0 = unspecified (optional)")] int leadTimeDays = 0,
        [Description("Valid from date/time (ISO 8601, optional)")] string? validFrom = null,
        [Description("Valid to date/time (ISO 8601, optional)")] string? validTo = null,
        [Description("Price breaks JSON array (optional)")] string? priceBreaks = null,
        [Description("Cost components JSON array (optional)")] string? costComponents = null,
        [Description("Participants JSON array (optional)")] string? participants = null,
        [Description("Certificates JSON array (optional)")] string? certificates = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(catalogRef))
        {
            return McpProtoHelper.FormatError("catalogRef is required");
        }

        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return McpProtoHelper.FormatError("sellerId is required");
        }

        if (string.IsNullOrWhiteSpace(originCountry))
        {
            return McpProtoHelper.FormatError("originCountry is required");
        }

        var request = new CreateSourcingOfferRequest
        {
            CatalogRef = new GuidValue(catalogRef),
            SellerCounterpartyId = new GuidValue(sellerId),
            OriginCountry = originCountry,
            OriginRegion = originRegion ?? string.Empty,
            OriginPlantation = originPlantation ?? string.Empty,
            OriginGrade = originGrade ?? string.Empty,
            Incoterms = incoterms ?? string.Empty,
            OriginHarvestYear = originHarvestYear,
            MinOrderQuantity = minOrderQuantity,
            LeadTimeDays = leadTimeDays,
        };

        if (!string.IsNullOrWhiteSpace(validFrom))
        {
            request.ValidFrom = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.Parse(validFrom, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(validTo))
        {
            request.ValidTo = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.Parse(validTo, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
        }

        var parseError = SourcingOfferInputHelper.PopulateCollections(
            request.PriceBreaks, request.CostComponents, request.Participants, request.Certificates,
            priceBreaks, costComponents, participants, certificates);

        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var offer = await client.CreateSourcingOfferAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSourcingOfferTool.MapSourcingOffer(offer), McpJsonDefaults.Options);
    }
}
