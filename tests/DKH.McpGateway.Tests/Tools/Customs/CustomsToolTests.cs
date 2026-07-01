using DKH.CustomsService.Contracts.Customs.Api.Declarations.v1;
using DKH.CustomsService.Contracts.Customs.Api.DocumentPackets.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;
using DKH.McpGateway.Application.Tools.Customs;
using DKH.Platform.Grpc.Common.Types;

namespace DKH.McpGateway.Tests.Tools.Customs;

public sealed class CustomsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly IApiKeyContext _readOnly = ApiKeyContextMocks.ReadOnly();

    private readonly DeclarationsService.DeclarationsServiceClient _declarations =
        Substitute.For<DeclarationsService.DeclarationsServiceClient>();

    private readonly DocumentPacketsService.DocumentPacketsServiceClient _documentPackets =
        Substitute.For<DocumentPacketsService.DocumentPacketsServiceClient>();

    private static readonly string DeclarationId = Guid.NewGuid().ToString();
    private static readonly string PacketId = Guid.NewGuid().ToString();

    [Fact]
    public async Task GetCustomsDeclaration_MissingId_ReturnsErrorAsync()
    {
        var result = await GetCustomsDeclarationTool.ExecuteAsync(_auth, _declarations, declarationId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("declarationId is required");
    }

    [Fact]
    public async Task GetCustomsDeclaration_HappyPath_ReturnsMappedDeclarationAsync()
    {
        _declarations.GetDeclarationAsync(
                Arg.Any<GetDeclarationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new DeclarationModel
            {
                Id = new GuidValue(DeclarationId),
                Type = DeclarationType.Import,
                Status = DeclarationStatus.Submitted,
                DestinationCountry = "CN",
                OriginCountry = "JP",
                Currency = "USD",
                DutiesTotal = 12.5,
            }));

        var result = await GetCustomsDeclarationTool.ExecuteAsync(_auth, _declarations, DeclarationId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(DeclarationId);
        json.GetProperty("type").GetString().Should().Be("Import");
        json.GetProperty("status").GetString().Should().Be("Submitted");
        json.GetProperty("destinationCountry").GetString().Should().Be("CN");
        json.GetProperty("originCountry").GetString().Should().Be("JP");
        json.GetProperty("dutiesTotal").GetDouble().Should().Be(12.5);
    }

    [Fact]
    public async Task GenerateStandardCustomsDocuments_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => GenerateStandardCustomsDocumentsTool.ExecuteAsync(
            _readOnly,
            _documentPackets,
            packetId: PacketId,
            jurisdictionCode: "CN",
            invoiceNumber: "INV-1",
            sellerName: "Seller",
            buyerName: "Buyer",
            currency: "USD",
            linesJson: "[]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GenerateStandardCustomsDocuments_OmitsContentBytesAsync()
    {
        _documentPackets.GenerateStandardDocumentsAsync(
                Arg.Any<GenerateStandardDocumentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GenerateStandardDocumentsResponse
            {
                Packet = new DocumentPacketModel
                {
                    Id = PacketId,
                    Status = DocumentPacketStatus.Draft,
                },
                Documents =
                {
                    new GeneratedCustomsDocument
                    {
                        ItemType = DocumentPacketItemType.CommercialInvoice,
                        FileName = "invoice.pdf",
                        ContentType = "application/pdf",
                        Sha256 = "hash",
                        DocumentRef = "storage://customs/invoice.pdf",
                        Content = Google.Protobuf.ByteString.CopyFrom([1, 2, 3]),
                    },
                },
            }));

        var result = await GenerateStandardCustomsDocumentsTool.ExecuteAsync(
            _auth,
            _documentPackets,
            packetId: PacketId,
            jurisdictionCode: "CN",
            invoiceNumber: "INV-1",
            sellerName: "Seller",
            buyerName: "Buyer",
            currency: "USD",
            linesJson: /*lang=json,strict*/ "[{\"description\":\"Tea\",\"hsCode\":\"0902\",\"quantity\":1,\"unitValue\":\"10\",\"grossWeightKg\":\"0.5\",\"netWeightKg\":\"0.4\",\"countryOfOrigin\":\"JP\"}]");

        var json = Parse(result);
        var document = json.GetProperty("documents")[0];
        document.GetProperty("fileName").GetString().Should().Be("invoice.pdf");
        document.GetProperty("documentRef").GetString().Should().Be("storage://customs/invoice.pdf");
        document.TryGetProperty("content", out _).Should().BeFalse();
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;
}
