using DKH.CustomsService.Contracts.Customs.Api.DocumentPackets.v1;
using Google.Protobuf.Collections;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class GenerateStandardCustomsDocumentsTool
{
    private static readonly JsonSerializerOptions InputJsonOptions = new(McpJsonDefaults.Options)
    {
        PropertyNameCaseInsensitive = true,
    };

    [McpServerTool(Name = "generate_standard_customs_documents"), Description(
        "Generate standard customs documents for a document packet. " +
        "linesJson: JSON array [{\"description\":\"Tea\",\"hsCode\":\"0902\",\"quantity\":1,\"unitValue\":\"10\",\"grossWeightKg\":\"0.5\",\"netWeightKg\":\"0.4\",\"countryOfOrigin\":\"JP\"}]. " +
        "The response intentionally omits generated document bytes.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DocumentPacketsService.DocumentPacketsServiceClient client,
        [Description("Document packet ID")] string packetId,
        [Description("Jurisdiction code, usually ISO-3166 alpha-2")] string jurisdictionCode,
        [Description("Commercial invoice number")] string invoiceNumber,
        [Description("Seller business name")] string sellerName,
        [Description("Buyer business name")] string buyerName,
        [Description("ISO-4217 currency code")] string currency,
        [Description("Customs document lines JSON array")] string linesJson,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(packetId))
        {
            return McpProtoHelper.FormatError("packetId is required");
        }

        if (string.IsNullOrWhiteSpace(jurisdictionCode))
        {
            return McpProtoHelper.FormatError("jurisdictionCode is required");
        }

        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return McpProtoHelper.FormatError("invoiceNumber is required");
        }

        if (string.IsNullOrWhiteSpace(sellerName))
        {
            return McpProtoHelper.FormatError("sellerName is required");
        }

        if (string.IsNullOrWhiteSpace(buyerName))
        {
            return McpProtoHelper.FormatError("buyerName is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        var request = new GenerateStandardDocumentsRequest
        {
            PacketId = packetId,
            JurisdictionCode = jurisdictionCode,
            InvoiceNumber = invoiceNumber,
            SellerName = sellerName,
            BuyerName = buyerName,
            Currency = currency,
        };

        var parseError = PopulateLines(request.Lines, linesJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var response = await client.GenerateStandardDocumentsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            packet = response.Packet is null ? null : CustomsMapper.MapPacket(response.Packet),
            documents = response.Documents.Select(CustomsMapper.MapGeneratedDocument),
        }, McpJsonDefaults.Options);
    }

    private static string? PopulateLines(RepeatedField<CustomsDocumentLine> destination, string? linesJson)
    {
        if (string.IsNullOrWhiteSpace(linesJson))
        {
            return "linesJson is required";
        }

        try
        {
            var lines = JsonSerializer.Deserialize<List<LineInput>>(linesJson, InputJsonOptions);
            if (lines is null || lines.Count == 0)
            {
                return "linesJson must contain at least one line";
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.Description))
                {
                    return "line.description is required";
                }

                if (string.IsNullOrWhiteSpace(line.HsCode))
                {
                    return "line.hsCode is required";
                }

                destination.Add(new CustomsDocumentLine
                {
                    Description = line.Description,
                    HsCode = line.HsCode,
                    Quantity = line.Quantity,
                    UnitValue = line.UnitValue ?? string.Empty,
                    GrossWeightKg = line.GrossWeightKg ?? string.Empty,
                    NetWeightKg = line.NetWeightKg ?? string.Empty,
                    CountryOfOrigin = line.CountryOfOrigin ?? string.Empty,
                });
            }
        }
        catch (JsonException)
        {
            return "linesJson is invalid JSON";
        }

        return null;
    }

    private sealed record LineInput(
        string Description,
        string HsCode,
        int Quantity,
        string? UnitValue,
        string? GrossWeightKg,
        string? NetWeightKg,
        string? CountryOfOrigin);
}
