using DKH.CustomsService.Contracts.Customs.Api.DocumentPackets.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;

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
}
