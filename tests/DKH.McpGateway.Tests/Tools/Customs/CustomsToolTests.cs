using DKH.CustomsService.Contracts.Customs.Api.Declarations.v1;
using DKH.CustomsService.Contracts.Customs.Api.DocumentPackets.v1;
using DKH.CustomsService.Contracts.Customs.Api.TradeRestrictions.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;
using DKH.CustomsService.Contracts.Duty.Api.V2;
using DKH.CustomsService.Contracts.DutyRule.Models.V2;
using DKH.CustomsService.Contracts.NationalHsCode.Api.Crud.V2;
using DKH.CustomsService.Contracts.NomenclatureSystem.Api.Crud.V2;
using DKH.CustomsService.Contracts.NomenclatureSystem.Models.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Api.Crud.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Models.V2;
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

    private readonly TradeRestrictionsService.TradeRestrictionsServiceClient _tradeRestrictions =
        Substitute.For<TradeRestrictionsService.TradeRestrictionsServiceClient>();

    private readonly DutyService.DutyServiceClient _duty =
        Substitute.For<DutyService.DutyServiceClient>();

    private readonly WcoHsCodeCrudService.WcoHsCodeCrudServiceClient _wcoHsCodes =
        Substitute.For<WcoHsCodeCrudService.WcoHsCodeCrudServiceClient>();

    private readonly NationalHsCodeCrudService.NationalHsCodeCrudServiceClient _nationalHsCodes =
        Substitute.For<NationalHsCodeCrudService.NationalHsCodeCrudServiceClient>();

    private readonly NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient _nomenclatureSystems =
        Substitute.For<NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient>();

    private static readonly string DeclarationId = Guid.NewGuid().ToString();
    private static readonly string PacketId = Guid.NewGuid().ToString();

    [Fact]
    public void CustomsToolSurface_DefinesEverySpecTool()
    {
        string[] expected =
        [
            "create_customs_declaration",
            "update_customs_declaration_items",
            "attach_customs_certificate",
            "submit_customs_declaration",
            "update_customs_declaration_status",
            "get_customs_declaration",
            "list_customs_declarations",
            "create_duty_rule",
            "update_duty_rule",
            "expire_duty_rule",
            "get_duty_rule",
            "list_duty_rules",
            "calculate_duties",
            "create_trade_restriction",
            "check_trade_restriction",
            "create_document_packet",
            "add_document_packet_item",
            "generate_standard_customs_documents",
            "compile_document_packet",
            "submit_document_packet",
            "acknowledge_document_packet",
            "get_document_packet",
            "create_wco_hs_code",
            "update_wco_hs_code",
            "retire_wco_hs_code",
            "reinstate_wco_hs_code",
            "get_wco_hs_code",
            "get_wco_hs_code_by_code",
            "list_wco_hs_codes",
            "get_wco_hs_hierarchy",
            "create_national_hs_code",
            "update_national_hs_code",
            "retire_national_hs_code",
            "reinstate_national_hs_code",
            "get_national_hs_code",
            "get_national_hs_code_by_full_code",
            "list_national_hs_codes",
            "get_national_hs_hierarchy",
            "create_nomenclature_system",
            "update_nomenclature_system",
            "get_nomenclature_system",
            "get_nomenclature_system_by_code",
            "list_nomenclature_systems",
        ];

        var actual = typeof(GetCustomsDeclarationTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Customs")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().Contain(expected);
    }

    [Fact]
    public async Task CreateCustomsDeclaration_MapsItemsAndOptionalGuidsAsync()
    {
        var fulfillmentId = Guid.NewGuid().ToString();
        var shipmentId = Guid.NewGuid().ToString();

        _declarations.CreateDeclarationAsync(
                Arg.Is<CreateDeclarationRequest>(request =>
                    request.Type == DeclarationType.Import &&
                    request.DestinationCountry == "CN" &&
                    request.OriginCountry == "JP" &&
                    request.FulfillmentId.Value == fulfillmentId &&
                    request.ShipmentId.Value == shipmentId &&
                    request.Items.Count == 1 &&
                    request.Items[0].HsCode == "0902" &&
                    request.Items[0].DeclaredValue == 10.5),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new DeclarationModel
            {
                Id = new GuidValue(DeclarationId),
                Type = DeclarationType.Import,
                Status = DeclarationStatus.Draft,
                DestinationCountry = "CN",
                OriginCountry = "JP",
                Currency = "USD",
            }));

        var result = await CreateCustomsDeclarationTool.ExecuteAsync(
            _auth,
            _declarations,
            type: "Import",
            destinationCountry: "CN",
            currency: "USD",
            itemsJson: /*lang=json,strict*/ "[{\"hsCode\":\"0902\",\"description\":\"Tea\",\"quantity\":1,\"declaredValue\":10.5}]",
            originCountry: "JP",
            fulfillmentId: fulfillmentId,
            shipmentId: shipmentId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(DeclarationId);
        json.GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task UpdateCustomsDeclarationStatus_RejectedWithoutReason_ReturnsErrorAsync()
    {
        var result = await UpdateCustomsDeclarationStatusTool.ExecuteAsync(
            _auth,
            _declarations,
            declarationId: DeclarationId,
            nextStatus: "Rejected");

        Parse(result).GetProperty("error").GetString().Should().Contain("reason is required");
    }

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

    [Fact]
    public async Task CalculateDuties_HappyPath_MapsRequestAndResultAsync()
    {
        _duty.CalculateDutiesAsync(
                Arg.Is<CalculateDutiesRequest>(request =>
                    request.DestinationCountry == "CN" &&
                    request.Lines.Count == 1 &&
                    request.Lines[0].HsCode == "0902" &&
                    request.Lines[0].ValueCurrency == "USD"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CalculateDutiesResult
            {
                TotalDuty = "1.25",
                Currency = "USD",
                Lines =
                {
                    new DutyLineResult
                    {
                        HsCode = "0902",
                        Origin = "JP",
                        Quantity = "1",
                        DeclaredValue = "10.00",
                        DutyAmount = "1.25",
                        Currency = "USD",
                        RuleId = "rule-1",
                        Matched = true,
                    },
                },
            }));

        var result = await CalculateCustomsDutiesTool.ExecuteAsync(
            _auth,
            _duty,
            destinationCountry: "CN",
            linesJson: /*lang=json,strict*/ "[{\"hsCode\":\"0902\",\"origin\":\"JP\",\"quantity\":\"1\",\"declaredValue\":\"10.00\",\"valueCurrency\":\"USD\"}]");

        var json = Parse(result);
        json.GetProperty("totalDuty").GetString().Should().Be("1.25");
        json.GetProperty("lines")[0].GetProperty("ruleId").GetString().Should().Be("rule-1");
    }

    [Fact]
    public async Task CalculateDuties_InvalidLinesJson_ReturnsErrorAsync()
    {
        var result = await CalculateCustomsDutiesTool.ExecuteAsync(_auth, _duty, "CN", "{broken");

        Parse(result).GetProperty("error").GetString().Should().Contain("linesJson is invalid JSON");
    }

    [Fact]
    public async Task ExpireDutyRule_ReturnsOkResponseAsync()
    {
        _duty.ExpireDutyRuleAsync(
                Arg.Is<ExpireDutyRuleRequest>(request => request.Id == "rule-1"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Google.Protobuf.WellKnownTypes.Empty()));

        var result = await ExpireDutyRuleTool.ExecuteAsync(_auth, _duty, id: "rule-1");

        var json = Parse(result);
        json.GetProperty("ok").GetBoolean().Should().BeTrue();
        json.GetProperty("id").GetString().Should().Be("rule-1");
    }

    [Fact]
    public async Task CheckTradeRestriction_HappyPath_MapsMatchAsync()
    {
        _tradeRestrictions.CheckTradeRestrictionAsync(
                Arg.Is<CheckTradeRestrictionRequest>(request =>
                    request.DestinationCountry == "CN" &&
                    request.OriginCountry == "JP" &&
                    request.HsCode == "090210"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CheckTradeRestrictionResponse
            {
                Match = new TradeRestrictionModel
                {
                    Id = "tr-1",
                    DestinationCountry = "CN",
                    OriginCountry = "JP",
                    HsCodePrefix = "0902",
                    Type = TradeRestrictionType.LicenseRequired,
                    Reason = "license",
                },
            }));

        var result = await CheckTradeRestrictionTool.ExecuteAsync(
            _auth,
            _tradeRestrictions,
            destinationCountry: "CN",
            hsCode: "090210",
            originCountry: "JP");

        var match = Parse(result).GetProperty("match");
        match.GetProperty("id").GetString().Should().Be("tr-1");
        match.GetProperty("type").GetString().Should().Be("LicenseRequired");
    }

    [Fact]
    public async Task ListWcoHsCodes_HappyPath_MapsTranslationsAsync()
    {
        _wcoHsCodes.ListWcoHsCodesAsync(
                Arg.Is<ListWcoHsCodesRequest>(request =>
                    request.Page == 2 &&
                    request.PageSize == 10 &&
                    request.Level == HsCodeLevel.Subheading &&
                    request.CodePrefix == "0902"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListWcoHsCodesResponse
            {
                TotalCount = 1,
                Page = 2,
                PageSize = 10,
                Items =
                {
                    new WcoHsCodeModel
                    {
                        Id = "wco-1",
                        Code = "0902",
                        Level = HsCodeLevel.Subheading,
                        HsRevision = "2022",
                        Translations =
                        {
                            new WcoHsCodeTranslationModel
                            {
                                LanguageCode = "en",
                                Description = "Tea",
                                ShortName = "Tea",
                            },
                        },
                    },
                },
            }));

        var result = await ListWcoHsCodesTool.ExecuteAsync(
            _auth,
            _wcoHsCodes,
            page: 2,
            pageSize: 10,
            level: "Subheading",
            codePrefix: "0902",
            hsRevision: "2022");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("items")[0].GetProperty("translations")[0].GetProperty("description").GetString().Should().Be("Tea");
    }

    [Fact]
    public async Task CreateWcoHsCode_ParsesTranslationsAsync()
    {
        _wcoHsCodes.CreateWcoHsCodeAsync(
                Arg.Is<CreateWcoHsCodeRequest>(request =>
                    request.Code == "0902" &&
                    request.Level == HsCodeLevel.Subheading &&
                    request.Translations.Count == 1 &&
                    request.Translations[0].LanguageCode == "en" &&
                    request.Translations[0].Description == "Tea"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new WcoHsCodeModel
            {
                Id = "wco-1",
                Code = "0902",
                Level = HsCodeLevel.Subheading,
                HsRevision = "2022",
            }));

        var result = await CreateWcoHsCodeTool.ExecuteAsync(
            _auth,
            _wcoHsCodes,
            code: "0902",
            level: "Subheading",
            hsRevision: "2022",
            translationsJson: /*lang=json,strict*/ "[{\"lang\":\"en\",\"description\":\"Tea\",\"shortName\":\"Tea\"}]");

        Parse(result).GetProperty("id").GetString().Should().Be("wco-1");
    }

    [Fact]
    public async Task GetNationalHsCodeByFullCode_MissingSystemCode_ReturnsErrorAsync()
    {
        var result = await GetNationalHsCodeByFullCodeTool.ExecuteAsync(_auth, _nationalHsCodes, systemCode: "", fullCode: "090210");

        Parse(result).GetProperty("error").GetString().Should().Contain("systemCode is required");
    }

    [Fact]
    public async Task CreateNomenclatureSystem_ParsesCountriesAndTranslationsAsync()
    {
        _nomenclatureSystems.CreateNomenclatureSystemAsync(
                Arg.Is<CreateNomenclatureSystemRequest>(request =>
                    request.Code == "CN" &&
                    request.Region == Region.Cn &&
                    request.IsoCountryCodes.Count == 2 &&
                    request.IsoCountryCodes[0] == "CN" &&
                    request.Translations.Count == 1 &&
                    request.Translations[0].LanguageCode == "en" &&
                    request.Translations[0].Name == "China HS"),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new NomenclatureSystemModel
            {
                Id = "system-1",
                Code = "CN",
                Region = Region.Cn,
                DigitsCount = 10,
                IsoCountryCodes = { "CN", "HK" },
            }));

        var result = await CreateNomenclatureSystemTool.ExecuteAsync(
            _auth,
            _nomenclatureSystems,
            code: "CN",
            region: "Cn",
            digitsCount: 10,
            sourceUrl: "https://example.test",
            currentRevision: "2024",
            isoCountryCodesJson: /*lang=json,strict*/ "[\"CN\",\"HK\"]",
            translationsJson: /*lang=json,strict*/ "[{\"lang\":\"en\",\"name\":\"China HS\",\"description\":\"China tariff system\"}]");

        Parse(result).GetProperty("code").GetString().Should().Be("CN");
    }

    [Fact]
    public async Task ListNomenclatureSystems_HappyPath_MapsRegionAsync()
    {
        _nomenclatureSystems.ListNomenclatureSystemsAsync(
                Arg.Is<ListNomenclatureSystemsRequest>(request => request.Region == Region.Cn),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListNomenclatureSystemsResponse
            {
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
                Items =
                {
                    new NomenclatureSystemModel
                    {
                        Id = "system-1",
                        Code = "CN",
                        Region = Region.Cn,
                        DigitsCount = 10,
                        IsoCountryCodes = { "CN" },
                        CurrentRevision = "2024",
                    },
                },
            }));

        var result = await ListNomenclatureSystemsTool.ExecuteAsync(
            _auth,
            _nomenclatureSystems,
            region: "Cn");

        var json = Parse(result);
        json.GetProperty("items")[0].GetProperty("code").GetString().Should().Be("CN");
        json.GetProperty("items")[0].GetProperty("region").GetString().Should().Be("Cn");
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;
}
