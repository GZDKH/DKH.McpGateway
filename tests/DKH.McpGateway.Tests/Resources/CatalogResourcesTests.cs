using DKH.McpGateway.Application.Resources;
using CatalogMgmt = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using CategoryMgmt = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using ProductMgmt = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

namespace DKH.McpGateway.Tests.Resources;

public class CatalogResourcesTests
{
    [Fact]
    public async Task GetCatalogs_Success_ReturnsJsonWithCatalogsAsync()
    {
        var client = Substitute.For<CatalogMgmt.CatalogManagementService.CatalogManagementServiceClient>();
        var response = new CatalogMgmt.GetStorefrontCatalogsResponse();
        client.GetCatalogsAsync(
                Arg.Any<CatalogMgmt.GetStorefrontCatalogsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var result = await CatalogResources.GetCatalogsAsync(client);

        var json = JsonDocument.Parse(result).RootElement;
        json.TryGetProperty("catalogs", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetCatalogs_PassesLanguageCodeAsync()
    {
        var client = Substitute.For<CatalogMgmt.CatalogManagementService.CatalogManagementServiceClient>();
        client.GetCatalogsAsync(
                Arg.Any<CatalogMgmt.GetStorefrontCatalogsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CatalogMgmt.GetStorefrontCatalogsResponse()));

        await CatalogResources.GetCatalogsAsync(client);

        _ = client.Received(1).GetCatalogsAsync(
            Arg.Is<CatalogMgmt.GetStorefrontCatalogsRequest>(r => r.LanguageCode == "ru"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategories_DefaultParams_UsesMainCatalogAsync()
    {
        var client = Substitute.For<CategoryMgmt.CategoryManagementService.CategoryManagementServiceClient>();
        client.GetCategoryTreeAsync(
                Arg.Any<CategoryMgmt.GetCategoryTreeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CategoryMgmt.CategoryTree()));

        var result = await CatalogResources.GetCategoriesAsync(client);

        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("catalog").GetString().Should().Be("main-catalog");

        _ = client.Received(1).GetCategoryTreeAsync(
            Arg.Is<CategoryMgmt.GetCategoryTreeRequest>(r =>
                r.CatalogSeoName == "main-catalog" && r.LanguageCode == "ru"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategories_CustomCatalog_UsesCatalogParamAsync()
    {
        var client = Substitute.For<CategoryMgmt.CategoryManagementService.CategoryManagementServiceClient>();
        client.GetCategoryTreeAsync(
                Arg.Any<CategoryMgmt.GetCategoryTreeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CategoryMgmt.CategoryTree()));

        var result = await CatalogResources.GetCategoriesAsync(client, catalogSeoName: "electronics");

        JsonDocument.Parse(result).RootElement.GetProperty("catalog").GetString()
            .Should().Be("electronics");
    }

    [Fact]
    public async Task GetProduct_Success_ReturnsProductDetailAsync()
    {
        var client = Substitute.For<ProductMgmt.ProductManagementService.ProductManagementServiceClient>();
        var product = new ProductMgmt.ProductDetail
        {
            Name = "Widget",
            SeoName = "widget",
            Code = "W-001",
            Description = "A great widget",
            Price = 99.99,
            CurrencyCode = "USD",
        };
        client.GetProductDetailAsync(
                Arg.Any<ProductMgmt.GetProductDetailRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(product));

        var result = await CatalogResources.GetProductAsync(
            client, productSeoName: "widget");

        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("name").GetString().Should().Be("Widget");
        json.GetProperty("code").GetString().Should().Be("W-001");
        json.GetProperty("seoName").GetString().Should().Be("widget");
    }

    [Fact]
    public async Task GetCatalogs_GrpcFailure_PropagatesAsync()
    {
        var client = Substitute.For<CatalogMgmt.CatalogManagementService.CatalogManagementServiceClient>();
        client.GetCatalogsAsync(
                Arg.Any<CatalogMgmt.GetStorefrontCatalogsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CatalogMgmt.GetStorefrontCatalogsResponse>(
                StatusCode.Unavailable));

        var act = () => CatalogResources.GetCatalogsAsync(client);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }
}
