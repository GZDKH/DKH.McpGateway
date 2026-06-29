using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetSourcingOfferTool
{
    [McpServerTool(Name = "get_sourcing_offer"), Description(
        "Get a sourcing offer by its ID. " +
        "Returns offer details including price breaks, cost components, participants, and certificates.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Sourcing offer ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var offer = await client.GetSourcingOfferAsync(
            new GetSourcingOfferRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapSourcingOffer(offer), McpJsonDefaults.Options);
    }

    internal static object MapSourcingOffer(SourcingOfferModel o) => new
    {
        id = o.Id?.Value,
        catalogRef = o.CatalogRef?.Value,
        sellerCounterpartyId = o.SellerCounterpartyId?.Value,
        originCountry = o.OriginCountry,
        originRegion = o.OriginRegion,
        originPlantation = o.OriginPlantation,
        originGrade = o.OriginGrade,
        incoterms = o.Incoterms,
        originHarvestYear = o.OriginHarvestYear,
        minOrderQuantity = o.MinOrderQuantity,
        leadTimeDays = o.LeadTimeDays,
        status = o.Status.ToString(),
        validFrom = o.ValidFrom?.ToDateTimeOffset().ToString("O"),
        validTo = o.ValidTo?.ToDateTimeOffset().ToString("O"),
        createdAt = o.CreatedAt?.ToDateTimeOffset().ToString("O"),
        priceBreaks = o.PriceBreaks.Select(b => new
        {
            id = b.Id?.Value,
            minQuantity = b.MinQuantity,
            unitPriceAmount = b.UnitPriceAmount?.ToDecimal(),
            unitPriceCurrency = b.UnitPriceCurrency,
        }),
        costComponents = o.CostComponents.Select(c => new
        {
            id = c.Id?.Value,
            kind = c.Kind.ToString(),
            amount = c.Amount?.ToDecimal(),
            currency = c.Currency,
        }),
        participants = o.Participants.Select(p => new
        {
            id = p.Id?.Value,
            counterpartyId = p.CounterpartyId?.Value,
            role = p.Role.ToString(),
        }),
        certificates = o.Certificates.Select(c => new
        {
            id = c.Id?.Value,
            certificateType = c.CertificateType,
            number = c.Number,
            issuerCounterpartyId = c.IssuerCounterpartyId?.Value,
            validFrom = c.ValidFrom?.ToDateTimeOffset().ToString("O"),
            validTo = c.ValidTo?.ToDateTimeOffset().ToString("O"),
            documentRef = c.DocumentRef?.Value,
        }),
    };
}
