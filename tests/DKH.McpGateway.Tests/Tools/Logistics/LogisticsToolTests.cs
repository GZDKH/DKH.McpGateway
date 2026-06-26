using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;
using DKH.McpGateway.Application.Tools.Logistics;

namespace DKH.McpGateway.Tests.Tools.Logistics;

public class LogisticsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly IApiKeyContext _readOnly = ApiKeyContextMocks.ReadOnly();

    private readonly QuoteService.QuoteServiceClient _quote =
        Substitute.For<QuoteService.QuoteServiceClient>();

    private readonly RateCardService.RateCardServiceClient _rateCard =
        Substitute.For<RateCardService.RateCardServiceClient>();

    private readonly SurchargeRuleService.SurchargeRuleServiceClient _surcharge =
        Substitute.For<SurchargeRuleService.SurchargeRuleServiceClient>();

    private readonly CarrierCapabilityService.CarrierCapabilityServiceClient _carrier =
        Substitute.For<CarrierCapabilityService.CarrierCapabilityServiceClient>();

    private static readonly string RateCardId = "rc-001";
    private static readonly string SurchargeId = "sr-001";
    private static readonly string CarrierId = "cap-001";
    private static readonly string CarrierCode = "DHL";

    // ---- CalculateRates ----

    [Fact]
    public async Task CalculateRates_MissingOrigin_ReturnsErrorAsync()
    {
        var result = await CalculateRatesTool.ExecuteAsync(
            _auth, _quote, originCountry: "", destinationCountry: "US");

        Parse(result).GetProperty("error").GetString().Should().Contain("originCountry is required");
    }

    [Fact]
    public async Task CalculateRates_MissingDestination_ReturnsErrorAsync()
    {
        var result = await CalculateRatesTool.ExecuteAsync(
            _auth, _quote, originCountry: "DE", destinationCountry: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("destinationCountry is required");
    }

    [Fact]
    public async Task CalculateRates_ReadOnly_AllowedAsync()
    {
        _quote.CalculateRatesAsync(
                Arg.Any<CalculateRatesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CalculateRatesResponse()));

        // Read permission allows CalculateRates
        var result = await CalculateRatesTool.ExecuteAsync(
            _readOnly, _quote, originCountry: "DE", destinationCountry: "US");

        Parse(result).TryGetProperty("options", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CalculateRates_HappyPath_ReturnsOptionsAsync()
    {
        var response = new CalculateRatesResponse();
        response.Options.Add(new RouteOptionModel
        {
            Carrier = "DHL",
            ServiceLevel = ServiceLevel.Standard,
            EstimatedTransitSeconds = 259200,
        });

        _quote.CalculateRatesAsync(
                Arg.Any<CalculateRatesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var result = await CalculateRatesTool.ExecuteAsync(
            _auth, _quote, originCountry: "DE", destinationCountry: "US");

        var json = Parse(result);
        json.GetProperty("options").GetArrayLength().Should().Be(1);
        json.GetProperty("options")[0].GetProperty("carrier").GetString().Should().Be("DHL");
        json.GetProperty("options")[0].GetProperty("serviceLevel").GetString().Should().Be("Standard");
    }

    [Fact]
    public async Task CalculateRates_InvalidParcelsJson_ReturnsErrorAsync()
    {
        var result = await CalculateRatesTool.ExecuteAsync(
            _auth, _quote, originCountry: "DE", destinationCountry: "US",
            parcels: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("parcels JSON is invalid");
    }

    // ---- GetRateCard ----

    [Fact]
    public async Task GetRateCard_MissingId_ReturnsErrorAsync()
    {
        var result = await GetRateCardTool.ExecuteAsync(_auth, _rateCard, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetRateCard_HappyPath_ReturnsCardAsync()
    {
        _rateCard.GetRateCardAsync(
                Arg.Any<GetRateCardRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new RateCardModel
            {
                Id = RateCardId,
                Name = "DHL Standard",
                Carrier = "DHL",
                ServiceLevel = ServiceLevel.Standard,
                Currency = "USD",
                Status = RateCardStatus.Draft,
            }));

        var result = await GetRateCardTool.ExecuteAsync(_auth, _rateCard, id: RateCardId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(RateCardId);
        json.GetProperty("carrier").GetString().Should().Be("DHL");
        json.GetProperty("status").GetString().Should().Be("Draft");
    }

    // ---- ListRateCards ----

    [Fact]
    public async Task ListRateCards_HappyPath_ReturnsPaginatedAsync()
    {
        _rateCard.ListRateCardsAsync(
                Arg.Any<ListRateCardsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListRateCardsResponse
            {
                TotalCount = 0,
                Page = 1,
                PageSize = 20,
            }));

        var result = await ListRateCardsTool.ExecuteAsync(_auth, _rateCard);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt64().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- CreateRateCard (write) ----

    [Fact]
    public async Task CreateRateCard_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateRateCardTool.ExecuteAsync(
            _readOnly, _rateCard,
            name: "DHL Standard",
            carrier: "DHL",
            serviceLevel: "Standard",
            currency: "USD",
            validityStartUnixMs: 0);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateRateCard_MissingName_ReturnsErrorAsync()
    {
        var result = await CreateRateCardTool.ExecuteAsync(
            _auth, _rateCard,
            name: "",
            carrier: "DHL",
            serviceLevel: "Standard",
            currency: "USD",
            validityStartUnixMs: 0);

        Parse(result).GetProperty("error").GetString().Should().Contain("name is required");
    }

    [Fact]
    public async Task CreateRateCard_InvalidTariffJson_ReturnsErrorAsync()
    {
        var result = await CreateRateCardTool.ExecuteAsync(
            _auth, _rateCard,
            name: "DHL Standard",
            carrier: "DHL",
            serviceLevel: "Standard",
            currency: "USD",
            validityStartUnixMs: 0,
            tariffRows: "bad-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("tariffRows JSON is invalid");
    }

    // ---- DeleteRateCard (write) ----

    [Fact]
    public async Task DeleteRateCard_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => DeleteRateCardTool.ExecuteAsync(_readOnly, _rateCard, id: RateCardId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteRateCard_MissingId_ReturnsErrorAsync()
    {
        var result = await DeleteRateCardTool.ExecuteAsync(_auth, _rateCard, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- ActivateRateCard (write) ----

    [Fact]
    public async Task ActivateRateCard_MissingId_ReturnsErrorAsync()
    {
        var result = await ActivateRateCardTool.ExecuteAsync(_auth, _rateCard, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- ExpireRateCard (write) ----

    [Fact]
    public async Task ExpireRateCard_MissingId_ReturnsErrorAsync()
    {
        var result = await ExpireRateCardTool.ExecuteAsync(_auth, _rateCard, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GetSurchargeRule ----

    [Fact]
    public async Task GetSurchargeRule_MissingId_ReturnsErrorAsync()
    {
        var result = await GetSurchargeRuleTool.ExecuteAsync(_auth, _surcharge, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetSurchargeRule_HappyPath_ReturnsRuleAsync()
    {
        _surcharge.GetSurchargeRuleAsync(
                Arg.Any<GetSurchargeRuleRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SurchargeRuleModel
            {
                Id = SurchargeId,
                Name = "Fuel Surcharge",
                Kind = SurchargeKind.Fuel,
                Mode = SurchargeCalculationMode.Percentage,
                Value = "0.08",
                IsActive = true,
            }));

        var result = await GetSurchargeRuleTool.ExecuteAsync(_auth, _surcharge, id: SurchargeId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(SurchargeId);
        json.GetProperty("kind").GetString().Should().Be("Fuel");
        json.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ---- ListSurchargeRules ----

    [Fact]
    public async Task ListSurchargeRules_HappyPath_ReturnsItemsAsync()
    {
        _surcharge.ListSurchargeRulesAsync(
                Arg.Any<ListSurchargeRulesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListSurchargeRulesResponse
            {
                TotalCount = 0,
                Page = 1,
                PageSize = 20,
            }));

        var result = await ListSurchargeRulesTool.ExecuteAsync(_auth, _surcharge);

        var json = Parse(result);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- CreateSurchargeRule (write) ----

    [Fact]
    public async Task CreateSurchargeRule_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateSurchargeRuleTool.ExecuteAsync(
            _readOnly, _surcharge,
            name: "Fuel",
            kind: "Fuel",
            mode: "Percentage",
            value: "0.08");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateSurchargeRule_MissingName_ReturnsErrorAsync()
    {
        var result = await CreateSurchargeRuleTool.ExecuteAsync(
            _auth, _surcharge,
            name: "",
            kind: "Fuel",
            mode: "Percentage",
            value: "0.08");

        Parse(result).GetProperty("error").GetString().Should().Contain("name is required");
    }

    // ---- DeleteSurchargeRule (write) ----

    [Fact]
    public async Task DeleteSurchargeRule_MissingId_ReturnsErrorAsync()
    {
        var result = await DeleteSurchargeRuleTool.ExecuteAsync(_auth, _surcharge, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- ActivateSurchargeRule (write) ----

    [Fact]
    public async Task ActivateSurchargeRule_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ActivateSurchargeRuleTool.ExecuteAsync(_readOnly, _surcharge, id: SurchargeId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ---- DeactivateSurchargeRule (write) ----

    [Fact]
    public async Task DeactivateSurchargeRule_MissingId_ReturnsErrorAsync()
    {
        var result = await DeactivateSurchargeRuleTool.ExecuteAsync(_auth, _surcharge, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GetCarrierCapability ----

    [Fact]
    public async Task GetCarrierCapability_MissingBoth_ReturnsErrorAsync()
    {
        var result = await GetCarrierCapabilityTool.ExecuteAsync(_auth, _carrier);

        Parse(result).GetProperty("error").GetString().Should().Contain("id or carrier is required");
    }

    [Fact]
    public async Task GetCarrierCapability_ById_ReturnsCapabilityAsync()
    {
        _carrier.GetCarrierCapabilityAsync(
                Arg.Any<GetCarrierCapabilityRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CarrierCapabilityModel
            {
                Id = CarrierId,
                Carrier = CarrierCode,
                DisplayName = "DHL Express",
                MaxLongestSideCm = "120",
            }));

        var result = await GetCarrierCapabilityTool.ExecuteAsync(_auth, _carrier, id: CarrierId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(CarrierId);
        json.GetProperty("carrier").GetString().Should().Be(CarrierCode);
        json.GetProperty("maxLongestSideCm").GetString().Should().Be("120");
    }

    // ---- ListCarrierCapabilities ----

    [Fact]
    public async Task ListCarrierCapabilities_HappyPath_ReturnsPaginatedAsync()
    {
        _carrier.ListCarrierCapabilitiesAsync(
                Arg.Any<ListCarrierCapabilitiesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListCarrierCapabilitiesResponse
            {
                TotalCount = 0,
                Page = 1,
                PageSize = 20,
            }));

        var result = await ListCarrierCapabilitiesTool.ExecuteAsync(_auth, _carrier);

        var json = Parse(result);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- RegisterCarrierCapability (write) ----

    [Fact]
    public async Task RegisterCarrierCapability_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => RegisterCarrierCapabilityTool.ExecuteAsync(
            _readOnly, _carrier,
            carrier: CarrierCode,
            displayName: "DHL Express",
            maxParcelWeightValue: "30",
            maxParcelWeightUnit: "Kilogram",
            maxLongestSideCm: "120");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RegisterCarrierCapability_MissingCarrier_ReturnsErrorAsync()
    {
        var result = await RegisterCarrierCapabilityTool.ExecuteAsync(
            _auth, _carrier,
            carrier: "",
            displayName: "DHL Express",
            maxParcelWeightValue: "30",
            maxParcelWeightUnit: "Kilogram",
            maxLongestSideCm: "120");

        Parse(result).GetProperty("error").GetString().Should().Contain("carrier is required");
    }

    // ---- DeleteCarrierCapability (write) ----

    [Fact]
    public async Task DeleteCarrierCapability_MissingId_ReturnsErrorAsync()
    {
        var result = await DeleteCarrierCapabilityTool.ExecuteAsync(_auth, _carrier, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- SetCarrierZoneMapping (write) ----

    [Fact]
    public async Task SetCarrierZoneMapping_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => SetCarrierZoneMappingTool.ExecuteAsync(
            _readOnly, _carrier, id: CarrierId, countryCode: "US", zone: "ZONE_A");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SetCarrierZoneMapping_MissingZone_ReturnsErrorAsync()
    {
        var result = await SetCarrierZoneMappingTool.ExecuteAsync(
            _auth, _carrier, id: CarrierId, countryCode: "US", zone: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("zone is required");
    }

    // ---- RemoveCarrierZoneMapping (write) ----

    [Fact]
    public async Task RemoveCarrierZoneMapping_MissingCountryCode_ReturnsErrorAsync()
    {
        var result = await RemoveCarrierZoneMappingTool.ExecuteAsync(
            _auth, _carrier, id: CarrierId, countryCode: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("countryCode is required");
    }

    [Fact]
    public async Task RemoveCarrierZoneMapping_MissingId_ReturnsErrorAsync()
    {
        var result = await RemoveCarrierZoneMappingTool.ExecuteAsync(
            _auth, _carrier, id: "", countryCode: "US");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- UpdateCarrierCapability (write) ----

    [Fact]
    public async Task UpdateCarrierCapability_MissingId_ReturnsErrorAsync()
    {
        var result = await UpdateCarrierCapabilityTool.ExecuteAsync(_auth, _carrier, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- UpdateRateCard (write) ----

    [Fact]
    public async Task UpdateRateCard_MissingId_ReturnsErrorAsync()
    {
        var result = await UpdateRateCardTool.ExecuteAsync(_auth, _rateCard, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- UpdateSurchargeRule (write) ----

    [Fact]
    public async Task UpdateSurchargeRule_MissingId_ReturnsErrorAsync()
    {
        var result = await UpdateSurchargeRuleTool.ExecuteAsync(_auth, _surcharge, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GrpcUnavailable propagates ----

    [Fact]
    public async Task ListRateCards_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _rateCard.ListRateCardsAsync(
                Arg.Any<ListRateCardsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListRateCardsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListRateCardsTool.ExecuteAsync(_auth, _rateCard);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
