using DKH.CustomsService.Contracts.Customs.Api.DocumentPackets.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class CreateDocumentPacketTool
{
    [McpServerTool(Name = "create_document_packet"), Description("Create a customs document packet for a fulfillment/shipment.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Fulfillment ID")] string fulfillmentId,
        [Description("Optional shipment ID")] string? shipmentId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        var packet = await client.CreateDocumentPacketAsync(
            new CreateDocumentPacketRequest { FulfillmentId = fulfillmentId, ShipmentId = shipmentId },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapPacket(packet), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class AddDocumentPacketItemTool
{
    [McpServerTool(Name = "add_document_packet_item"), Description("Add a document reference to a customs document packet.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Document packet ID")] string packetId,
        [Description("DocumentPacketItemType enum name")] string itemType,
        [Description("Document reference")] string documentRef,
        [Description("Optional item description")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(packetId))
        {
            return McpProtoHelper.FormatError("packetId is required");
        }

        if (!CustomsToolInput.TryParseEnum<DocumentPacketItemType>(itemType, out var parsedType))
        {
            return McpProtoHelper.FormatError("itemType is invalid");
        }

        if (string.IsNullOrWhiteSpace(documentRef))
        {
            return McpProtoHelper.FormatError("documentRef is required");
        }

        var packet = await client.AddItemAsync(
            new AddItemRequest
            {
                PacketId = packetId,
                ItemType = parsedType,
                DocumentRef = documentRef,
                Description = description,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapPacket(packet), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class CompileDocumentPacketTool
{
    [McpServerTool(Name = "compile_document_packet"), Description("Compile a customs document packet.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Document packet ID")] string packetId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(packetId))
        {
            return McpProtoHelper.FormatError("packetId is required");
        }

        var packet = await client.CompilePacketAsync(
            new CompilePacketRequest { PacketId = packetId },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapPacket(packet), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class SubmitDocumentPacketTool
{
    [McpServerTool(Name = "submit_document_packet"), Description("Submit a customs document packet with an optional filing reference.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Document packet ID")] string packetId,
        [Description("Optional external filing reference")] string? filingReference = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(packetId))
        {
            return McpProtoHelper.FormatError("packetId is required");
        }

        var packet = await client.SubmitPacketAsync(
            new SubmitPacketRequest { PacketId = packetId, FilingReference = filingReference },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapPacket(packet), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class AcknowledgeDocumentPacketTool
{
    [McpServerTool(Name = "acknowledge_document_packet"), Description("Mark a submitted customs document packet as acknowledged.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Document packet ID")] string packetId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(packetId))
        {
            return McpProtoHelper.FormatError("packetId is required");
        }

        var packet = await client.AcknowledgePacketAsync(
            new AcknowledgePacketRequest { PacketId = packetId },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapPacket(packet), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetDocumentPacketTool
{
    [McpServerTool(Name = "get_document_packet"), Description("Get a customs document packet by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Document packet ID")] string packetId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(packetId))
        {
            return McpProtoHelper.FormatError("packetId is required");
        }

        var packet = await client.GetDocumentPacketAsync(
            new GetDocumentPacketRequest { PacketId = packetId },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapPacket(packet), McpJsonDefaults.Options);
    }
}
