using DKH.McpGateway.Application.Tools.Products;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class AskExpertToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly ProductSearchService.ProductSearchServiceClient _client =
        Substitute.For<ProductSearchService.ProductSearchServiceClient>();

    [Fact]
    public async Task AskExpert_ReturnsRankedRecommendationsAsync()
    {
        var response = new SearchProductsResponse { Found = 2 };
        response.Hits.Add(Hit("sencha", "Sencha"));
        response.Hits.Add(Hit("gyokuro", "Gyokuro"));
        SetupSearch(response);

        var result = await ExecuteToolAsync("a light green tea for mornings");

        var json = Parse(result);
        json.GetProperty("need").GetString().Should().Be("a light green tea for mornings");
        json.GetProperty("recommendationCount").GetInt32().Should().Be(2);
        var recs = json.GetProperty("recommendations");
        recs.GetArrayLength().Should().Be(2);
        recs[0].GetProperty("rank").GetInt32().Should().Be(1);
        recs[0].GetProperty("seoName").GetString().Should().Be("sencha");
        recs[1].GetProperty("rank").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task AskExpert_UsesSemanticVectorQueryAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("something fruity", limit: 4);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.Query == "something fruity" &&
                r.VectorQuery.Contains("embedding:([]") &&
                r.PerPage == 4),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AskExpert_WithPriceMax_EchoesCriteriaAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        var result = await ExecuteToolAsync("budget tea", priceMax: 25.0);

        Parse(result).GetProperty("criteria").GetProperty("priceMax").GetDouble().Should().Be(25.0);
    }

    [Fact]
    public async Task AskExpert_NonCommercial_HidesPriceAsync()
    {
        var response = new SearchProductsResponse { Found = 1 };
        response.Hits.Add(Hit("sencha", "Sencha", price: 14.0f, currency: "USD"));
        SetupSearch(response);

        var result = await ExecuteToolAsync("green tea", nonCommercial: true);

        var rec = Parse(result).GetProperty("recommendations")[0];
        rec.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        rec.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task AskExpert_LimitClamped_To20Async()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("tea", limit: 100);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PerPage == 20),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    private static SearchHitModel Hit(string seoName, string name, float price = 0f, string currency = "")
        => new()
        {
            Document = new ProductSearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Code = seoName.ToUpperInvariant(),
                Name = name,
                SeoName = seoName,
                Price = price,
                Currency = currency,
            },
        };

    private Task<string> ExecuteToolAsync(
        string need,
        string? catalogSeoName = null,
        string lang = "ru",
        string? brandFilter = null,
        double? priceMax = null,
        int limit = 3,
        bool nonCommercial = false)
        => AskExpertTool.ExecuteAsync(
            _auth,
            _client,
            need: need,
            catalogSeoName: catalogSeoName,
            lang: lang,
            brandFilter: brandFilter,
            priceMax: priceMax,
            limit: limit,
            nonCommercial: nonCommercial);

    private void SetupSearch(SearchProductsResponse response)
        => _client.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
