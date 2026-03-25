using DKH.McpGateway.Application.Tools.Products;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class SearchProductsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly ProductSearchService.ProductSearchServiceClient _client =
        Substitute.For<ProductSearchService.ProductSearchServiceClient>();

    [Fact]
    public async Task SearchProducts_HappyPath_ReturnsResultsAsync()
    {
        var response = new SearchProductsResponse
        {
            Found = 1,
            Page = 1,
        };
        response.Hits.Add(new SearchHitModel
        {
            Document = new ProductSearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Code = "PROD-001",
                Name = "Test Product",
                SeoName = "test-product",
                Price = 29.99f,
                Currency = "USD",
                Brand = "TestBrand",
                InStock = true,
            },
        });
        SetupSearch(response);

        var result = await ExecuteToolAsync("test");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("page").GetInt32().Should().Be(1);
        json.GetProperty("pageSize").GetInt32().Should().Be(20);
        json.GetProperty("products").GetArrayLength().Should().Be(1);

        var product = json.GetProperty("products")[0];
        product.GetProperty("code").GetString().Should().Be("PROD-001");
        product.GetProperty("name").GetString().Should().Be("Test Product");
        product.GetProperty("seoName").GetString().Should().Be("test-product");
        product.GetProperty("brand").GetString().Should().Be("TestBrand");
        product.GetProperty("inStock").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SearchProducts_EmptyResults_ReturnsEmptyListAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0, Page = 1 });

        var result = await ExecuteToolAsync("nonexistent");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("products").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SearchProducts_WithBrandFilter_SetsFilterByAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("test", brandFilter: "brand-a,brand-b");

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.FilterBy.Contains("brand:=[`brand-a`,`brand-b`]")),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_WithPriceRange_SetsFilterByAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("test", priceMin: 10.0, priceMax: 100.0);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.FilterBy.Contains("price:[10..100]")),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_PageSizeClamped_To100Async()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("test", pageSize: 200);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PerPage == 100),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_PageSizeClamped_To1Async()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("test", pageSize: 0);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PerPage == 1),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_WithSemanticSearch_SetsVectorQueryAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("natural language query", semanticSearch: true);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.VectorQuery.Contains("embedding:([]")),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_WithoutSemanticSearch_NoVectorQueryAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("keyword query");

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                string.IsNullOrEmpty(r.VectorQuery)),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_CallForPrice_ReturnsNullPriceAsync()
    {
        var response = new SearchProductsResponse { Found = 1 };
        response.Hits.Add(new SearchHitModel
        {
            Document = new ProductSearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Code = "CALL",
                Name = "Call For Price Product",
                SeoName = "call-product",
                CallForPrice = true,
                Price = 99.99f,
            },
        });
        SetupSearch(response);

        var result = await ExecuteToolAsync("call");

        var product = Parse(result).GetProperty("products")[0];
        product.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task SearchProducts_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<SearchProductsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("test");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string query,
        string? catalogSeoName = null,
        string? languageCode = null,
        string? brandFilter = null,
        double? priceMin = null,
        double? priceMax = null,
        bool semanticSearch = false,
        int page = 1,
        int pageSize = 20)
        => SearchProductsTool.ExecuteAsync(
            _auth,
            _client,
            query: query,
            catalogSeoName: catalogSeoName,
            languageCode: languageCode,
            brandFilter: brandFilter,
            priceMin: priceMin,
            priceMax: priceMax,
            semanticSearch: semanticSearch,
            page: page,
            pageSize: pageSize);

    private void SetupSearch(SearchProductsResponse response)
        => _client.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
