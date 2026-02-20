using DKH.McpGateway.Application.Resources;
using Microsoft.Extensions.Caching.Memory;
using CountryMgmt = DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using CurrencyMgmt = DKH.ReferenceService.Contracts.Reference.Api.CurrencyManagement.v1;
using LanguageMgmt = DKH.ReferenceService.Contracts.Reference.Api.LanguageManagement.v1;

namespace DKH.McpGateway.Tests.Resources;

public class ReferenceResourcesTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetCountries_Success_ReturnsJsonWithCountriesAsync()
    {
        var client = Substitute.For<CountryMgmt.CountryManagementService.CountryManagementServiceClient>();
        var response = new CountryMgmt.ListCountriesResponse();
        client.ListAsync(
                Arg.Any<CountryMgmt.ListCountriesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var result = await ReferenceResources.GetCountriesAsync(client, _cache);

        var json = JsonDocument.Parse(result).RootElement;
        json.TryGetProperty("countries", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetCountries_PassesLanguageAndPageSizeAsync()
    {
        var client = Substitute.For<CountryMgmt.CountryManagementService.CountryManagementServiceClient>();
        client.ListAsync(
                Arg.Any<CountryMgmt.ListCountriesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CountryMgmt.ListCountriesResponse()));

        await ReferenceResources.GetCountriesAsync(client, _cache, languageCode: "en-US");

        _ = client.Received(1).ListAsync(
            Arg.Is<CountryMgmt.ListCountriesRequest>(r =>
                r.Language == "en-US" && r.PageSize == 1000),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCountryByCode_Success_ReturnsDetailsAsync()
    {
        var client = Substitute.For<CountryMgmt.CountryManagementService.CountryManagementServiceClient>();
        var country = new CountryMgmt.CountryModel
        {
            TwoLetterCode = "US",
            ThreeLetterCode = "USA",
            NumericCode = 840,
        };
        client.GetAsync(
                Arg.Any<CountryMgmt.GetCountryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(country));

        var result = await ReferenceResources.GetCountryByCodeAsync(client, _cache, countryCode: "US");

        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("code").GetString().Should().Be("US");
        json.GetProperty("code3").GetString().Should().Be("USA");
    }

    [Fact]
    public async Task GetCurrencies_Success_ReturnsJsonWithCurrenciesAsync()
    {
        var client = Substitute.For<CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient>();
        client.ListAsync(
                Arg.Any<CurrencyMgmt.ListCurrenciesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CurrencyMgmt.ListCurrenciesResponse()));

        var result = await ReferenceResources.GetCurrenciesAsync(client, _cache);

        JsonDocument.Parse(result).RootElement
            .TryGetProperty("currencies", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetLanguages_Success_ReturnsJsonWithLanguagesAsync()
    {
        var client = Substitute.For<LanguageMgmt.LanguageManagementService.LanguageManagementServiceClient>();
        client.ListAsync(
                Arg.Any<LanguageMgmt.ListLanguagesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new LanguageMgmt.ListLanguagesResponse()));

        var result = await ReferenceResources.GetLanguagesAsync(client, _cache);

        JsonDocument.Parse(result).RootElement
            .TryGetProperty("languages", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetCountries_GrpcFailure_PropagatesAsync()
    {
        var client = Substitute.For<CountryMgmt.CountryManagementService.CountryManagementServiceClient>();
        client.ListAsync(
                Arg.Any<CountryMgmt.ListCountriesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CountryMgmt.ListCountriesResponse>(
                StatusCode.Unavailable));

        var act = () => ReferenceResources.GetCountriesAsync(client, _cache);

        await act.Should().ThrowAsync<RpcException>();
    }
}
