using DKH.McpGateway.Application.Tools.Products;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class SimilarProductsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly ProductManagementService.ProductManagementServiceClient _catalog =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();
    private readonly ProductSearchService.ProductSearchServiceClient _search =
        Substitute.For<ProductSearchService.ProductSearchServiceClient>();

    [Fact]
    public async Task SimilarProducts_ExcludesSourceAndReturnsSimilarAsync()
    {
        SetupSource(new ProductDetail
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Code = "SRC",
            Name = "Earl Grey",
            SeoName = "earl-grey",
            Description = "A bergamot black tea",
        });

        var response = new SearchProductsResponse { Found = 2 };
        response.Hits.Add(Hit("earl-grey", "Earl Grey"));
        response.Hits.Add(Hit("lady-grey", "Lady Grey"));
        SetupSearch(response);

        var result = await ExecuteToolAsync("earl-grey");

        var similar = Parse(result).GetProperty("similar");
        similar.GetArrayLength().Should().Be(1);
        similar[0].GetProperty("seoName").GetString().Should().Be("lady-grey");
    }

    [Fact]
    public async Task SimilarProducts_BuildsSensoryQueryAndVectorAsync()
    {
        SetupSource(new ProductDetail
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Code = "SRC",
            Name = "Earl Grey",
            SeoName = "earl-grey",
            Description = "bergamot black tea",
        });
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("earl-grey", limit: 5);

        _ = _search.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.Query.Contains("Earl Grey") &&
                r.VectorQuery.Contains("embedding:([]") &&
                r.PerPage == 6),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SimilarProducts_LimitClamped_To50Async()
    {
        SetupSource(new ProductDetail { Code = "SRC", Name = "Tea", SeoName = "tea" });
        SetupSearch(new SearchProductsResponse { Found = 0 });

        await ExecuteToolAsync("tea", limit: 999);

        _ = _search.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PerPage == 51),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SimilarProducts_NonCommercial_HidesPriceAsync()
    {
        SetupSource(new ProductDetail { Code = "SRC", Name = "Tea", SeoName = "tea" });
        var response = new SearchProductsResponse { Found = 1 };
        response.Hits.Add(Hit("other-tea", "Other Tea", price: 9.9f, currency: "USD"));
        SetupSearch(response);

        var result = await ExecuteToolAsync("tea", nonCommercial: true);

        var similar = Parse(result).GetProperty("similar")[0];
        similar.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        similar.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
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
        string productSeoName,
        string catalogSeoName = "main-catalog",
        string lang = "ru",
        int limit = 6,
        bool nonCommercial = false)
        => SimilarProductsTool.ExecuteAsync(
            _auth,
            _catalog,
            _search,
            productSeoName: productSeoName,
            catalogSeoName: catalogSeoName,
            lang: lang,
            limit: limit,
            nonCommercial: nonCommercial);

    private void SetupSource(ProductDetail detail)
        => _catalog.GetProductDetailAsync(
                Arg.Any<GetProductDetailRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(detail));

    private void SetupSearch(SearchProductsResponse response)
        => _search.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
