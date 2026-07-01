using DKH.CustomsService.Contracts.Customs.Api.DocumentPackets.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;
using DKH.CustomsService.Contracts.DutyRule.Models.V2;
using DKH.CustomsService.Contracts.NationalHsCode.Models.V2;
using DKH.CustomsService.Contracts.NomenclatureSystem.Models.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Models.V2;

namespace DKH.McpGateway.Application.Tools.Customs;

internal static class CustomsMapper
{
    internal static object MapDeclaration(DeclarationModel d) => new
    {
        id = d.Id?.Value,
        type = d.Type.ToString(),
        status = d.Status.ToString(),
        destinationCountry = d.DestinationCountry,
        originCountry = d.OriginCountry,
        currency = d.Currency,
        dutiesTotal = d.DutiesTotal,
        fulfillmentId = d.FulfillmentId?.Value,
        shipmentId = d.ShipmentId?.Value,
        brokerPartnerId = d.BrokerPartnerId?.Value,
        filingReference = d.FilingReference,
        rejectionReason = d.RejectionReason,
        submittedAt = d.SubmittedAt?.ToDateTimeOffset().ToString("O"),
        acceptedAt = d.AcceptedAt?.ToDateTimeOffset().ToString("O"),
        clearedAt = d.ClearedAt?.ToDateTimeOffset().ToString("O"),
        rejectedAt = d.RejectedAt?.ToDateTimeOffset().ToString("O"),
        items = d.Items.Select(MapDeclarationItem),
        certificates = d.Certificates.Select(MapCertificate),
    };

    internal static object MapPacket(DocumentPacketModel p) => new
    {
        id = p.Id,
        fulfillmentId = p.FulfillmentId,
        shipmentId = p.ShipmentId,
        status = p.Status.ToString(),
        filingReference = p.FilingReference,
        creationTime = p.CreationTime?.ToDateTimeOffset().ToString("O"),
        items = p.Items.Select(MapPacketItem),
    };

    internal static object MapGeneratedDocument(GeneratedCustomsDocument g) => new
    {
        itemType = g.ItemType.ToString(),
        fileName = g.FileName,
        contentType = g.ContentType,
        sha256 = g.Sha256,
        documentRef = g.DocumentRef,
    };

    internal static object MapDutyRule(DutyRuleModel r) => new
    {
        id = r.Id,
        ruleType = r.RuleType.ToString(),
        nomenclatureSystemId = r.NomenclatureSystemId,
        systemCode = r.SystemCode,
        hsCodePrefix = r.HsCodePrefix,
        destinationCountry = r.DestinationCountry,
        originCountry = r.OriginCountry,
        percentOfValue = r.PercentOfValue,
        fixedAmountPerUnit = r.FixedAmountPerUnit,
        currency = r.Currency,
        validFrom = r.ValidFrom?.ToDateTimeOffset().ToString("O"),
        validUntil = r.ValidUntil?.ToDateTimeOffset().ToString("O"),
        legalReference = r.LegalReference,
    };

    internal static object MapDutyCalculation(CalculateDutiesResult result) => new
    {
        lines = result.Lines.Select(MapDutyLineResult),
        totalDuty = result.TotalDuty,
        currency = result.Currency,
    };

    internal static object MapWcoHsCode(WcoHsCodeModel code) => new
    {
        id = code.Id,
        code = code.Code,
        level = code.Level.ToString(),
        parentCode = code.ParentCode,
        hsRevision = code.HsRevision,
        notes = code.Notes,
        isRetired = code.IsRetired,
        translations = code.Translations.Select(t => new
        {
            languageCode = t.LanguageCode,
            description = t.Description,
            shortName = t.ShortName,
        }),
    };

    internal static object MapWcoHierarchyNode(HierarchyNode node) => new
    {
        value = node.Value is null ? null : MapWcoHsCode(node.Value),
        children = node.Children.Select(MapWcoHierarchyNode),
    };

    internal static object MapNationalHsCode(NationalHsCodeModel code) => new
    {
        id = code.Id,
        nomenclatureSystemId = code.NomenclatureSystemId,
        systemCode = code.SystemCode,
        wcoHsCodeId = code.WcoHsCodeId,
        wcoCode = code.WcoCode,
        extendedCode = code.ExtendedCode,
        fullCode = code.FullCode,
        level = code.Level.ToString(),
        notes = code.Notes,
        validFrom = code.ValidFrom?.ToDateTimeOffset().ToString("O"),
        validUntil = code.ValidUntil?.ToDateTimeOffset().ToString("O"),
        isRetired = code.IsRetired,
        translations = code.Translations.Select(t => new
        {
            languageCode = t.LanguageCode,
            description = t.Description,
            shortName = t.ShortName,
        }),
    };

    internal static object MapNationalHierarchyNode(NationalHierarchyNode node) => new
    {
        value = node.Value is null ? null : MapNationalHsCode(node.Value),
        children = node.Children.Select(MapNationalHierarchyNode),
    };

    internal static object MapNomenclatureSystem(NomenclatureSystemModel system) => new
    {
        id = system.Id,
        code = system.Code,
        region = system.Region.ToString(),
        digitsCount = system.DigitsCount,
        isoCountryCodes = system.IsoCountryCodes.ToArray(),
        sourceUrl = system.SourceUrl,
        currentRevision = system.CurrentRevision,
        lastImportedAt = system.LastImportedAt?.ToDateTimeOffset().ToString("O"),
        translations = system.Translations.Select(t => new
        {
            languageCode = t.LanguageCode,
            name = t.Name,
            description = t.Description,
        }),
    };

    private static object MapDeclarationItem(DeclarationItemModel i) => new
    {
        id = i.Id?.Value,
        hsCode = i.HsCode,
        description = i.Description,
        quantity = i.Quantity,
        declaredValue = i.DeclaredValue,
        currency = i.Currency,
        computedDuty = i.ComputedDuty,
    };

    private static object MapCertificate(CertificateModel c) => new
    {
        id = c.Id?.Value,
        declarationId = c.DeclarationId?.Value,
        type = c.Type.ToString(),
        issuingAuthority = c.IssuingAuthority,
        documentRef = c.DocumentRef,
        validFrom = c.ValidFrom?.ToDateTimeOffset().ToString("O"),
        validUntil = c.ValidUntil?.ToDateTimeOffset().ToString("O"),
    };

    private static object MapPacketItem(DocumentPacketItemModel i) => new
    {
        id = i.Id,
        packetId = i.PacketId,
        itemType = i.ItemType.ToString(),
        documentRef = i.DocumentRef,
        description = i.Description,
        attachedAt = i.AttachedAt?.ToDateTimeOffset().ToString("O"),
    };

    private static object MapDutyLineResult(DutyLineResult line) => new
    {
        hsCode = line.HsCode,
        origin = line.Origin,
        quantity = line.Quantity,
        declaredValue = line.DeclaredValue,
        dutyAmount = line.DutyAmount,
        currency = line.Currency,
        ruleId = line.RuleId,
        matched = line.Matched,
    };
}
