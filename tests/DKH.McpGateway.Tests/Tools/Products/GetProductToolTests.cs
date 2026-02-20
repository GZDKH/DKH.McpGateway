using DKH.McpGateway.Application.Tools.Products;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class GetProductToolTests
{
    private readonly ProductManagementService.ProductManagementServiceClient _client =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();

    [Fact]
    public async Task GetProduct_HappyPath_ReturnsProductDetailAsync()
    {
        var detail = CreateProductDetail();
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        var json = Parse(result);
        json.GetProperty("code").GetString().Should().Be("PROD-001");
        json.GetProperty("name").GetString().Should().Be("Test Product");
        json.GetProperty("seoName").GetString().Should().Be("test-product");
        json.GetProperty("description").GetString().Should().Be("A test product description");
        json.GetProperty("currency").GetString().Should().Be("USD");
    }

    [Fact]
    public async Task GetProduct_WithBrand_ReturnsBrandNameAsync()
    {
        var detail = CreateProductDetail();
        detail.Brand = new QueryBrand { Name = "TestBrand", SeoName = "test-brand" };
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        Parse(result).GetProperty("brand").GetString().Should().Be("TestBrand");
    }

    [Fact]
    public async Task GetProduct_WithManufacturer_ReturnsManufacturerNameAsync()
    {
        var detail = CreateProductDetail();
        detail.Manufacturer = new QueryManufacturer { Name = "TestMfg", SeoName = "test-mfg" };
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        Parse(result).GetProperty("manufacturer").GetString().Should().Be("TestMfg");
    }

    [Fact]
    public async Task GetProduct_WithCategories_ReturnsCategoryListAsync()
    {
        var detail = CreateProductDetail();
        detail.Categories.Add(new QueryCatalogCategory
        {
            CategoryName = "Electronics",
            CategorySeoName = "electronics",
        });
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        var categories = Parse(result).GetProperty("categories");
        categories.GetArrayLength().Should().Be(1);
        categories[0].GetProperty("name").GetString().Should().Be("Electronics");
        categories[0].GetProperty("seoName").GetString().Should().Be("electronics");
    }

    [Fact]
    public async Task GetProduct_WithTags_ReturnsTagNamesAsync()
    {
        var detail = CreateProductDetail();
        detail.Tags.Add(new QueryTag { Name = "Sale" });
        detail.Tags.Add(new QueryTag { Name = "New" });
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        var tags = Parse(result).GetProperty("tags");
        tags.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetProduct_WithSpecifications_ReturnsGroupedSpecsAsync()
    {
        var detail = CreateProductDetail();
        var group = new QuerySpecificationGroup
        {
            Code = "general",
            Name = "General",
        };
        group.Items.Add(new QuerySpecificationItem
        {
            Code = "weight",
            Name = "Weight",
            Value = "500",
            Unit = "g",
        });
        detail.Specifications.Add(group);
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        var specs = Parse(result).GetProperty("specifications");
        specs.GetArrayLength().Should().Be(1);
        specs[0].GetProperty("group").GetString().Should().Be("General");
        specs[0].GetProperty("items").GetArrayLength().Should().Be(1);
        specs[0].GetProperty("items")[0].GetProperty("attribute").GetString().Should().Be("Weight");
        specs[0].GetProperty("items")[0].GetProperty("value").GetString().Should().Be("500");
        specs[0].GetProperty("items")[0].GetProperty("unit").GetString().Should().Be("g");
    }

    [Fact]
    public async Task GetProduct_WithVariants_ReturnsVariantCombinationsAsync()
    {
        var detail = CreateProductDetail();
        detail.VariantCombinations.Add(new QueryVariantCombination
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Sku = "PROD-001-RED",
            PriceAdjustment = 5.0,
            StockQuantity = 10,
        });
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        var variants = Parse(result).GetProperty("variants");
        variants.GetArrayLength().Should().Be(1);
        variants[0].GetProperty("sku").GetString().Should().Be("PROD-001-RED");
        variants[0].GetProperty("priceAdjustment").GetDouble().Should().Be(5.0);
        variants[0].GetProperty("stockQuantity").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task GetProduct_WithOrigin_ReturnsOriginAsync()
    {
        var detail = CreateProductDetail();
        detail.Origin = new QueryProductOrigin
        {
            CountryCode = "US",
            StateProvinceCode = "CA",
        };
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        var origin = Parse(result).GetProperty("origin");
        origin.GetProperty("countryCode").GetString().Should().Be("US");
        origin.GetProperty("stateProvinceCode").GetString().Should().Be("CA");
    }

    [Fact]
    public async Task GetProduct_NoOrigin_ReturnsNullOriginAsync()
    {
        var detail = CreateProductDetail();
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        Parse(result).GetProperty("origin").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetProduct_CallForPrice_ReturnsNullPriceAsync()
    {
        var detail = CreateProductDetail();
        detail.CallForPrice = true;
        detail.Price = 99.99;
        SetupGetProductDetail(detail);

        var result = await ExecuteToolAsync("test-product");

        Parse(result).GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetProduct_SetsRequestFieldsAsync()
    {
        SetupGetProductDetail(CreateProductDetail());

        await ExecuteToolAsync("my-product", catalogSeoName: "special-catalog", languageCode: "en");

        _ = _client.Received(1).GetProductDetailAsync(
            Arg.Is<GetProductDetailRequest>(r =>
                r.ProductSeoName == "my-product" &&
                r.CatalogSeoName == "special-catalog" &&
                r.LanguageCode == "en"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProduct_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetProductDetailAsync(
                Arg.Any<GetProductDetailRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ProductDetail>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("test-product");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task GetProduct_NotFound_ThrowsRpcExceptionAsync()
    {
        _client.GetProductDetailAsync(
                Arg.Any<GetProductDetailRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ProductDetail>(
                StatusCode.NotFound, "Product not found"));

        var act = () => ExecuteToolAsync("nonexistent");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.NotFound);
    }

    private static ProductDetail CreateProductDetail() => new()
    {
        Id = new GuidValue(Guid.NewGuid().ToString()),
        Code = "PROD-001",
        Name = "Test Product",
        SeoName = "test-product",
        Description = "A test product description",
        Price = 29.99,
        CurrencyCode = "USD",
        Published = true,
    };

    private Task<string> ExecuteToolAsync(
        string productSeoName,
        string catalogSeoName = "main-catalog",
        string languageCode = "ru")
        => GetProductTool.ExecuteAsync(
            _client,
            productSeoName: productSeoName,
            catalogSeoName: catalogSeoName,
            languageCode: languageCode);

    private void SetupGetProductDetail(ProductDetail response)
        => _client.GetProductDetailAsync(
                Arg.Any<GetProductDetailRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
