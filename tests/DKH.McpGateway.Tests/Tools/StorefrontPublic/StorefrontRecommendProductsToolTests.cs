using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Application.Tools.StorefrontPublic;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Catalog.v1;

namespace DKH.McpGateway.Tests.Tools.StorefrontPublic;

public class StorefrontRecommendProductsToolTests
{
    private static readonly Guid Storefront = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string SeedId = "11111111-1111-1111-1111-111111111111";

    private readonly IStorefrontMcpGate _gate = Substitute.For<IStorefrontMcpGate>();

    private readonly StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient _storefrontCatalogClient =
        Substitute.For<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();

    private readonly CatalogManagementService.CatalogManagementServiceClient _catalogClient =
        Substitute.For<CatalogManagementService.CatalogManagementServiceClient>();

    private readonly ProductManagementService.ProductManagementServiceClient _productClient =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();

    private readonly ProductSearchService.ProductSearchServiceClient _searchClient =
        Substitute.For<ProductSearchService.ProductSearchServiceClient>();

    public StorefrontRecommendProductsToolTests()
    {
        // Storefront bound to a single visible catalog "catalog-a" (the isolation boundary).
        _storefrontCatalogClient.GetCatalogsAsync(
                Arg.Any<GetCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse
            {
                Catalogs = { new StorefrontCatalogModel { CatalogId = new GuidValue("cat-a"), IsVisible = true } },
            }));

        _catalogClient.GetCatalogsAsync(
                Arg.Any<GetStorefrontCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontCatalogsResponse
            {
                Catalogs = { new StorefrontCatalogItem { Name = "Catalog A", SeoName = "catalog-a" } },
            }));
    }

    [Fact]
    public async Task Recommend_HappyPath_ReturnsNeighbours_ExcludingSeedAsync()
    {
        SetupSeedFoundInCatalog("catalog-a");
        SetupRecommendations(
            Rec(SeedId, "SEED", distance: 0.0),       // the seed itself — must be filtered out
            Rec("22222222-2222-2222-2222-222222222222", "NEAR", distance: 0.1),
            Rec("33333333-3333-3333-3333-333333333333", "FAR", distance: 0.5));

        var json = Parse(await ExecuteAsync("seed-product"));

        json.GetProperty("seedSeoName").GetString().Should().Be("seed-product");
        json.GetProperty("seedProductId").GetString().Should().Be(SeedId);

        var recs = json.GetProperty("recommendations");
        recs.GetArrayLength().Should().Be(2);
        recs[0].GetProperty("code").GetString().Should().Be("NEAR"); // ordered by ascending distance
        recs[1].GetProperty("code").GetString().Should().Be("FAR");
        recs.EnumerateArray().Select(r => r.GetProperty("id").GetString()).Should().NotContain(SeedId);
    }

    [Fact]
    public async Task Recommend_SeedNotInStorefrontCatalogs_ThrowsNotFound_AndNeverRecommendsAsync()
    {
        // Seed product is unreachable from this storefront's catalogs → it must not seed recommendations
        // (a storefront key can never base "more like this" on another tenant's product — ADR-060).
        _productClient.GetProductDetailAsync(
                Arg.Any<GetProductDetailRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ProductDetail>(
                StatusCode.NotFound, "not in this catalog"));

        var act = () => ExecuteAsync("foreign-product");

        await act.Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.NotFound);

        _ = _searchClient.DidNotReceive().GetRecommendationsAsync(
            Arg.Any<GetRecommendationsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recommend_FiltersToStorefrontCatalog_AndSeedsByResolvedIdAsync()
    {
        SetupSeedFoundInCatalog("catalog-a");
        SetupRecommendations();

        await ExecuteAsync("seed-product");

        _ = _searchClient.Received(1).GetRecommendationsAsync(
            Arg.Is<GetRecommendationsRequest>(r =>
                r.ProductId == SeedId && r.FilterBy.Contains("catalog_seo:=`catalog-a`")),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recommend_MaxResultsClamped_To50Async()
    {
        SetupSeedFoundInCatalog("catalog-a");
        SetupRecommendations();

        await ExecuteAsync("seed-product", maxResults: 500);

        _ = _searchClient.Received(1).GetRecommendationsAsync(
            Arg.Is<GetRecommendationsRequest>(r => r.MaxResults == 50),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    private void SetupSeedFoundInCatalog(string catalogSeoName)
        => _productClient.GetProductDetailAsync(
                Arg.Is<GetProductDetailRequest>(r => r.CatalogSeoName == catalogSeoName),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductDetail
            {
                Id = new GuidValue(SeedId),
                Code = "SEED",
                Name = "Seed Product",
                SeoName = "seed-product",
            }));

    private void SetupRecommendations(params RecommendationModel[] recommendations)
    {
        var response = new GetRecommendationsResponse();
        response.Recommendations.AddRange(recommendations);
        _searchClient.GetRecommendationsAsync(
                Arg.Any<GetRecommendationsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));
    }

    private static RecommendationModel Rec(string id, string code, double distance) => new()
    {
        Product = new ProductSearchModel { Id = id, Code = code, Name = code, SeoName = code.ToLowerInvariant() },
        VectorDistance = distance,
    };

    private Task<string> ExecuteAsync(string productSeoName, int maxResults = 10)
        => StorefrontRecommendProductsTool.ExecuteAsync(
            StorefrontKey(Storefront),
            _gate,
            _storefrontCatalogClient,
            _catalogClient,
            _productClient,
            _searchClient,
            productSeoName: productSeoName,
            languageCode: "en",
            maxResults: maxResults);

    private static IApiKeyContext StorefrontKey(Guid storefrontId)
    {
        var ctx = Substitute.For<IApiKeyContext>();
        ctx.Scope.Returns(ApiKeyScope.Storefront);
        ctx.StorefrontId.Returns(storefrontId);
        return ctx;
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
