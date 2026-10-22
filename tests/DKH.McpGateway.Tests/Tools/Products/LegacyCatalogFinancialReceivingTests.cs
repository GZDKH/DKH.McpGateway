using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;
using DKH.McpGateway.Application.Resources;
using DKH.McpGateway.Application.Tools.Products;
using DKH.McpGateway.Application.Tools.StorefrontPublic;
using DKH.McpGateway.Tests.Tools.Storefronts;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Catalog.v1;
using Google.Protobuf;

namespace DKH.McpGateway.Tests.Tools.Products;

public sealed class LegacyCatalogFinancialReceivingTests
{
    private static readonly Guid Storefront = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Theory]
    [InlineData("generic", false)]
    [InlineData("resource", false)]
    [InlineData("storefront", false)]
    [InlineData("generic", true)]
    [InlineData("resource", true)]
    [InlineData("storefront", true)]
    public async Task RetainedCatalogValuesDoNotBecomeCommercialPriceOrAvailabilityAsync(string route, bool nondefault)
    {
        var detail = new ProductDetail
        {
            Id = new GuidValue("22222222-2222-2222-2222-222222222222"),
            Code = "TEA-001",
            Name = "Tea",
            SeoName = "tea",
            Description = "Product content",
            Price = nondefault ? 29.75 : 0,
            CurrencyCode = nondefault ? "USD" : "",
            Published = true,
        };
        detail.Categories.Add(new QueryCatalogCategory { CategoryName = "Green", CategorySeoName = "green" });
        detail.VariantCombinations.Add(new QueryVariantCombination
        {
            Id = new GuidValue("33333333-3333-3333-3333-333333333333"),
            Sku = "TEA-GREEN",
            PriceAdjustment = nondefault ? 5 : 0,
            StockQuantity = nondefault ? 10 : 0,
        });
        var received = ProductDetail.Parser.ParseFrom(detail.Clone().ToByteArray());
        using var cts = new CancellationTokenSource();
        var client = Substitute.For<ProductManagementService.ProductManagementServiceClient>();
        Metadata? headers = null;
        client.GetProductDetailAsync(Arg.Any<GetProductDetailRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.ArgAt<GetProductDetailRequest>(0);
                request.CatalogSeoName.Should().Be("catalog-a");
                request.ProductSeoName.Should().Be("tea");
                request.LanguageCode.Should().Be("en");
                call.ArgAt<CancellationToken>(3).Should().Be(cts.Token);
                headers = call.ArgAt<Metadata?>(1);
                return GrpcTestHelpers.CreateAsyncUnaryCall(received);
            });
        var counterparty = Substitute.For<CounterpartyCrudService.CounterpartyCrudServiceClient>();
        var auth = ApiKeyContextMocks.FullAccess();
        string result;
        if (route == "generic")
        {
            result = await GetProductTool.ExecuteAsync(auth, StorefrontWorkspaceTestContext.HttpContextAccessor,
                client, counterparty, "tea", "catalog-a", "en", cts.Token);
        }
        else if (route == "resource")
        {
            result = await CatalogResources.GetProductAsync(auth, StorefrontWorkspaceTestContext.HttpContextAccessor,
                client, counterparty, "tea", "catalog-a", "en", cts.Token);
        }
        else
        {
            auth = Substitute.For<IApiKeyContext>();
            auth.Scope.Returns(ApiKeyScope.Storefront);
            auth.StorefrontId.Returns(Storefront);
            var gate = Substitute.For<IStorefrontMcpGate>();
            var links = Substitute.For<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();
            links.GetCatalogsAsync(Arg.Any<GetCatalogsRequest>(), Arg.Any<Metadata>(),
                    Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse
                {
                    Catalogs = { new StorefrontCatalogModel { CatalogId = new GuidValue("catalog-id"), IsVisible = true } },
                }));
            var catalogs = Substitute.For<CatalogManagementService.CatalogManagementServiceClient>();
            catalogs.GetCatalogsAsync(Arg.Any<GetStorefrontCatalogsRequest>(), Arg.Any<Metadata>(),
                    Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontCatalogsResponse
                {
                    Catalogs = { new StorefrontCatalogItem { SeoName = "catalog-a", Name = "Catalog" } },
                }));
            result = await StorefrontGetProductTool.ExecuteAsync(auth, gate, links, catalogs, client,
                counterparty, "tea", "en", cts.Token);
            await gate.Received(1).EnsureMcpEnabledAsync(Storefront, cts.Token);
            _ = links.Received(1).GetCatalogsAsync(
                Arg.Is<GetCatalogsRequest>(request => request.StorefrontId.Value == Storefront.ToString("D")),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), cts.Token);
        }

        using var json = JsonDocument.Parse(result);
        var output = json.RootElement;
        output.GetProperty("code").GetString().Should().Be("TEA-001");
        output.GetProperty("name").GetString().Should().Be("Tea");
        output.GetProperty("description").GetString().Should().Be("Product content");
        output.GetProperty("categories")[0].GetProperty("seoName").GetString().Should().Be("green");
        output.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        output.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
        if (route == "generic")
        {
            var variant = output.GetProperty("variants")[0];
            variant.GetProperty("sku").GetString().Should().Be("TEA-GREEN");
            variant.GetProperty("priceAdjustment").ValueKind.Should().Be(JsonValueKind.Null);
            variant.GetProperty("stockQuantity").ValueKind.Should().Be(JsonValueKind.Null);
        }

        if (route != "storefront")
        {
            headers.Should().NotBeNull();
            headers!.Single(header => header.Key == "x-workspace-id").Value.Should()
                .Be(StorefrontWorkspaceTestContext.WorkspaceId.ToString("D"));
            auth.Received().EnsurePermission(McpPermissions.Read);
        }
    }
}
