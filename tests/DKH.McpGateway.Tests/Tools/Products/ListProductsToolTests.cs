using DKH.McpGateway.Application.Tools.Products;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class ListProductsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly ProductSearchService.ProductSearchServiceClient _client =
        Substitute.For<ProductSearchService.ProductSearchServiceClient>();

    [Fact]
    public async Task ListProducts_HappyPath_ReturnsProductsAsync()
    {
        var response = new SearchProductsResponse { Found = 1, Page = 1 };
        response.Hits.Add(new SearchHitModel
        {
            Document = new ProductSearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Code = "PROD-001",
                Name = "Test Product",
                SeoName = "test-product",
                Price = 12.5f,
                Currency = "USD",
                Brand = "TestBrand",
                InStock = true,
            },
        });
        SetupSearch(response);

        var result = await ExecuteToolAsync();

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("products").GetArrayLength().Should().Be(1);
        json.GetProperty("products")[0].GetProperty("seoName").GetString().Should().Be("test-product");
    }

    [Fact]
    public async Task ListProducts_UsesMatchAllQueryAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync();

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.Query == "*" && string.IsNullOrEmpty(r.VectorQuery)),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListProducts_WithBrandFilter_SetsFilterByAsync()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync(brandFilter: "brand-a,brand-b");

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.FilterBy.Contains("brand:=[`brand-a`,`brand-b`]")),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListProducts_PageSizeClamped_To100Async()
    {
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync(pageSize: 500);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PerPage == 100),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListProducts_NonCommercial_HidesPriceAsync()
    {
        var response = new SearchProductsResponse { Found = 1 };
        response.Hits.Add(new SearchHitModel
        {
            Document = new ProductSearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Code = "PROD-001",
                Name = "Test Product",
                SeoName = "test-product",
                Price = 12.5f,
                Currency = "USD",
            },
        });
        SetupSearch(response);

        var result = await ExecuteToolAsync(nonCommercial: true);

        var product = Parse(result).GetProperty("products")[0];
        product.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        product.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private Task<string> ExecuteToolAsync(
        string? catalogSeoName = null,
        string? lang = null,
        string? brandFilter = null,
        double? priceMin = null,
        double? priceMax = null,
        int page = 1,
        int pageSize = 20,
        bool nonCommercial = false)
        => ListProductsTool.ExecuteAsync(
            _auth,
            _client,
            catalogSeoName: catalogSeoName,
            lang: lang,
            brandFilter: brandFilter,
            priceMin: priceMin,
            priceMax: priceMax,
            page: page,
            pageSize: pageSize,
            nonCommercial: nonCommercial);

    private void SetupSearch(SearchProductsResponse response)
        => _client.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
