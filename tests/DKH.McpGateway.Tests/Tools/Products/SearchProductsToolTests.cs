using DKH.McpGateway.Application.Tools.Products;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.QueryCommon.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class SearchProductsToolTests
{
    private readonly ProductManagementService.ProductManagementServiceClient _client =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();

    [Fact]
    public async Task SearchProducts_HappyPath_ReturnsResultsAsync()
    {
        var response = new SearchProductsResponse
        {
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
        };
        response.Items.Add(new ProductListItem
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Code = "PROD-001",
            Name = "Test Product",
            SeoName = "test-product",
            Price = 29.99,
            CurrencyCode = "USD",
            BrandName = "TestBrand",
            InStock = true,
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
        SetupSearch(new SearchProductsResponse { TotalCount = 0, Page = 1, PageSize = 20 });

        var result = await ExecuteToolAsync("nonexistent");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("products").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SearchProducts_WithBrandFilter_SetsBrandSeoNamesAsync()
    {
        SetupSearch(new SearchProductsResponse { TotalCount = 0 });

        await ExecuteToolAsync("test", brandFilter: "brand-a,brand-b");

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.BrandSeoNames.Count == 2 &&
                r.BrandSeoNames[0] == "brand-a" &&
                r.BrandSeoNames[1] == "brand-b"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_WithPriceRange_SetsPriceFiltersAsync()
    {
        SetupSearch(new SearchProductsResponse { TotalCount = 0 });

        await ExecuteToolAsync("test", priceMin: 10.0, priceMax: 100.0);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.MinPrice == 10.0 &&
                r.MaxPrice == 100.0),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_PageSizeClamped_To100Async()
    {
        SetupSearch(new SearchProductsResponse { TotalCount = 0 });

        await ExecuteToolAsync("test", pageSize: 200);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PageSize == 100),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_PageSizeClamped_To1Async()
    {
        SetupSearch(new SearchProductsResponse { TotalCount = 0 });

        await ExecuteToolAsync("test", pageSize: 0);

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r => r.PageSize == 1),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_DefaultParameters_UsesDefaultsAsync()
    {
        SetupSearch(new SearchProductsResponse { TotalCount = 0 });

        await ExecuteToolAsync("test");

        _ = _client.Received(1).SearchProductsAsync(
            Arg.Is<SearchProductsRequest>(r =>
                r.CatalogSeoName == "main-catalog" &&
                r.LanguageCode == "ru" &&
                r.Page == 1 &&
                r.PageSize == 20),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_CallForPrice_ReturnsNullPriceAsync()
    {
        var response = new SearchProductsResponse { TotalCount = 1 };
        response.Items.Add(new ProductListItem
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Code = "CALL",
            Name = "Call For Price Product",
            SeoName = "call-product",
            CallForPrice = true,
            Price = 99.99,
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
        string catalogSeoName = "main-catalog",
        string languageCode = "ru",
        string? brandFilter = null,
        double? priceMin = null,
        double? priceMax = null,
        int page = 1,
        int pageSize = 20)
        => SearchProductsTool.ExecuteAsync(
            _client,
            query: query,
            catalogSeoName: catalogSeoName,
            languageCode: languageCode,
            brandFilter: brandFilter,
            priceMin: priceMin,
            priceMax: priceMax,
            page: page,
            pageSize: pageSize);

    private void SetupSearch(SearchProductsResponse response)
        => _client.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
