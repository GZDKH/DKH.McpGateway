using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Application.Tools.Products;
using DKH.McpGateway.Application.Tools.StorefrontPublic;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Catalog.v1;
using Google.Protobuf;
using SearchProductsRequest = DKH.SearchService.Contracts.Search.Api.ProductSearch.v1.SearchProductsRequest;
using SearchProductsResponse = DKH.SearchService.Contracts.Search.Api.ProductSearch.v1.SearchProductsResponse;

namespace DKH.McpGateway.Tests.Tools.Products;

public sealed class SearchPresenceReceivingTests
{
    private static readonly Guid Storefront = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Theory]
    [InlineData("generic")]
    [InlineData("storefront")]
    [InlineData("recommendations")]
    public async Task OmittedValuesRemainUnknownThroughActualToolsAsync(string route)
    {
        var product = new ProductSearchModel
        {
            Id = "22222222-2222-2222-2222-222222222222",
            Code = "TEA-TEST",
            Name = "Test tea",
        };
        var output = await ExecuteAsync(route, product);

        using var json = JsonDocument.Parse(output);
        var item = json.RootElement.GetProperty(route == "recommendations" ? "recommendations" : "products")[0];
        item.GetProperty("id").GetString().Should().Be(product.Id);
        item.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("inStock").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.Null);
    }
    [Theory]
    [InlineData("price")]
    [InlineData("original_price")]
    [InlineData("currency")]
    [InlineData("in_stock")]
    [InlineData("min_variant_price")]
    [InlineData("max_variant_price")]
    [InlineData("call_for_price")]
    public async Task IndependentExplicitDefaultsSurviveCloneBinaryAndClearThroughEveryToolAsync(string name)
    {
        var product = new ProductSearchModel { Id = "22222222-2222-2222-2222-222222222222", Code = "TEA-TEST", Name = "Test tea" };
        var field = ProductSearchModel.Descriptor.FindFieldByName(name);
        object value = field.FieldType switch
        {
            Google.Protobuf.Reflection.FieldType.Float => 0f,
            Google.Protobuf.Reflection.FieldType.Bool => false,
            Google.Protobuf.Reflection.FieldType.String => string.Empty,
            _ => throw new InvalidOperationException("Unexpected financial field type"),
        };
        field.Accessor.SetValue(product, value);
        var supplied = ProductSearchModel.Parser.ParseFrom(product.Clone().ToByteArray());
        field.Accessor.HasValue(supplied).Should().BeTrue();
        foreach (var route in new[] { "generic", "storefront", "recommendations" })
        {
            using var output = JsonDocument.Parse(await ExecuteAsync(route, supplied));
            var item = output.RootElement.GetProperty(route == "recommendations" ? "recommendations" : "products")[0];
            if (name is not ("price" or "currency" or "in_stock"))
            {
                item.TryGetProperty(field.JsonName, out _).Should().BeFalse("unexposed SDK fields must not expand the tool surface");
                continue;
            }

            var json = item.GetProperty(field.JsonName);
            if (value is float)
            {
                json.GetDouble().Should().Be(0);
            }
            else if (value is bool)
            {
                json.GetBoolean().Should().BeFalse();
            }
            else
            {
                json.GetString().Should().BeEmpty();
            }
        }

        field.Accessor.Clear(supplied);
        var cleared = ProductSearchModel.Parser.ParseFrom(supplied.ToByteArray());
        field.Accessor.HasValue(cleared).Should().BeFalse();
        foreach (var route in new[] { "generic", "storefront", "recommendations" })
        {
            using var output = JsonDocument.Parse(await ExecuteAsync(route, cleared));
            var item = output.RootElement.GetProperty(route == "recommendations" ? "recommendations" : "products")[0];
            if (name is "price" or "currency" or "in_stock")
            {
                item.GetProperty(field.JsonName).ValueKind.Should().Be(JsonValueKind.Null);
            }
            else
            {
                item.TryGetProperty(field.JsonName, out _).Should().BeFalse();
            }
        }
    }

    [Theory]
    [InlineData(10.0, null)]
    [InlineData(null, 100.0)]
    public async Task LegacyPriceControlIsRefusedBeforeRpcAsync(double? minimum, double? maximum)
    {
        var client = Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        var response = new SearchProductsResponse();
        client.SearchProductsAsync(Arg.Any<SearchProductsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var execute = () => SearchProductsTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), client,
            "tea", catalogSeoName: "catalog-a", priceMin: minimum, priceMax: maximum);

        await execute.Should().ThrowAsync<ArgumentException>();
        client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task MissingGenericScopeIsRefusedBeforeRpcAsync()
    {
        var client = Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        client.SearchProductsAsync(Arg.Any<SearchProductsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SearchProductsResponse()));

        var execute = () => SearchProductsTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), client, "tea");

        await execute.Should().ThrowAsync<ArgumentException>();
        client.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData("generic")]
    [InlineData("storefront")]
    public async Task SemanticUsesSupportedBoundedRpcAndExactScopeAsync(string route)
    {
        using var cancellation = new CancellationTokenSource();
        var client = Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        var response = new HybridSearchResponse { Found = 0, Page = 1, PerPage = 20 };
        client.HybridSearchAsync(Arg.Any<HybridSearchRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(HybridSearchResponse.Parser.ParseFrom(response.ToByteArray())));
        client.SearchProductsAsync(Arg.Any<SearchProductsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SearchProductsResponse()));
        await ExecuteAsync(route, new ProductSearchModel(), semantic: true, searchOverride: client, token: cancellation.Token);

        _ = client.Received(1).HybridSearchAsync(Arg.Is<HybridSearchRequest>(request =>
                request.Mode == SearchMode.Semantic && request.Page == 1 && request.PerPage == 20 &&
                request.MaxResults == 20 && request.PerEngineTopK >= 20 && request.PerEngineTopK <= 100 &&
                request.PerEngineTimeoutMs > 0 && request.PerEngineTimeoutMs <= 1000 &&
                request.FilterBy == "catalog_seo:=`catalog-a`"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Is<CancellationToken>(token => token == cancellation.Token));
        _ = client.DidNotReceive().SearchProductsAsync(Arg.Any<SearchProductsRequest>(), Arg.Any<Metadata>(),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("generic", false)]
    [InlineData("storefront", false)]
    [InlineData("recommendations", false)]
    [InlineData("generic", true)]
    [InlineData("storefront", true)]
    [InlineData("recommendations", true)]
    public async Task SuppliedValuesAndQuotePolicyPreserveExistingJsonBoundaryAsync(string route, bool quote)
    {
        var product = new ProductSearchModel
        {
            Id = "22222222-2222-2222-2222-222222222222",
            Code = "TEA-TEST",
            Name = "Test tea",
            Price = 1.25f,
            OriginalPrice = 2.5f,
            Currency = "CNY",
            InStock = true,
            MinVariantPrice = 1f,
            MaxVariantPrice = 3f,
            CallForPrice = quote,
        };
        using var output = JsonDocument.Parse(await ExecuteAsync(route, product));
        var item = output.RootElement.GetProperty(route == "recommendations" ? "recommendations" : "products")[0];
        if (quote)
        {
            item.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        }
        else
        {
            item.GetProperty("price").GetDouble().Should().Be(1.25);
        }

        item.GetProperty("currency").GetString().Should().Be("CNY");
        item.GetProperty("inStock").GetBoolean().Should().BeTrue();
        item.GetProperty("seoName").GetString().Should().Be(product.Code);
        item.EnumerateObject().Count().Should().Be(route == "recommendations" ? 9 : 8);
        foreach (var field in new[] { "originalPrice", "minVariantPrice", "maxVariantPrice", "callForPrice" })
        {
            item.TryGetProperty(field, out _).Should().BeFalse("the existing tool disclosure boundary is retained");
        }
    }

    [Theory]
    [InlineData(2, 100)]
    [InlineData(int.MaxValue, 100)]
    [InlineData(0, 20)]
    public async Task InvalidSemanticWindowFailsBeforeRpcAsync(int page, int pageSize)
    {
        var client = Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        var execute = () => SearchProductsTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), client,
            "tea", catalogSeoName: "catalog-a", semanticSearch: true, page: page, pageSize: pageSize);

        await execute.Should().ThrowAsync<ArgumentOutOfRangeException>();
        client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ExistingReadPermissionRemainsBeforeSearchAsync()
    {
        var client = Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        var execute = () => SearchProductsTool.ExecuteAsync(ApiKeyContextMocks.Unauthenticated(), client,
            "tea", catalogSeoName: "catalog-a");

        await execute.Should().ThrowAsync<UnauthorizedAccessException>();
        client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ExistingStorefrontGateRemainsBeforeSearchAsync()
    {
        var client = Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        var gate = Substitute.For<IStorefrontMcpGate>();
        gate.EnsureMcpEnabledAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new UnauthorizedAccessException("MCP disabled for declared test storefront")));
        var execute = () => ExecuteAsync("storefront", new ProductSearchModel(),
            searchOverride: client, gateOverride: gate);

        await execute.Should().ThrowAsync<UnauthorizedAccessException>();
        client.ReceivedCalls().Should().BeEmpty();
    }

    private static async Task<string> ExecuteAsync(string route, ProductSearchModel product,
        bool semantic = false, ProductSearchService.ProductSearchServiceClient? searchOverride = null,
        IStorefrontMcpGate? gateOverride = null, CancellationToken token = default)
    {
        var client = searchOverride ?? Substitute.For<ProductSearchService.ProductSearchServiceClient>();
        var response = new SearchProductsResponse { Found = 1 };
        response.Hits.Add(new SearchHitModel { Document = product.Clone() });
        client.SearchProductsAsync(Arg.Any<SearchProductsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(SearchProductsResponse.Parser.ParseFrom(response.ToByteArray())));
        var recommendations = new GetRecommendationsResponse();
        recommendations.Recommendations.Add(new RecommendationModel { Product = product.Clone(), VectorDistance = 0.25 });
        client.GetRecommendationsAsync(Arg.Any<GetRecommendationsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(
                GetRecommendationsResponse.Parser.ParseFrom(recommendations.ToByteArray())));
        var gate = gateOverride ?? Substitute.For<IStorefrontMcpGate>();
        var bindings = Substitute.For<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();
        bindings.GetCatalogsAsync(Arg.Any<GetCatalogsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse
            {
                Catalogs = { new StorefrontCatalogModel { CatalogId = new GuidValue("cat-a"), IsVisible = true } },
            }));
        var catalogs = Substitute.For<CatalogManagementService.CatalogManagementServiceClient>();
        catalogs.GetCatalogsAsync(Arg.Any<GetStorefrontCatalogsRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontCatalogsResponse
            {
                Catalogs = { new StorefrontCatalogItem { Name = "Test catalog", SeoName = "catalog-a" } },
            }));
        var products = Substitute.For<ProductManagementService.ProductManagementServiceClient>();
        products.GetProductDetailAsync(Arg.Any<GetProductDetailRequest>(), Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductDetail
            {
                Id = new GuidValue("11111111-1111-1111-1111-111111111111"),
                Code = "SEED",
                Name = "Seed",
                SeoName = "seed",
            }));
        var scoped = Substitute.For<IApiKeyContext>();
        scoped.Scope.Returns(ApiKeyScope.Storefront);
        scoped.StorefrontId.Returns(Storefront);

        return route switch
        {
            "generic" => await SearchProductsTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), client,
                "tea", catalogSeoName: "catalog-a", semanticSearch: semantic, cancellationToken: token),
            "storefront" => await StorefrontSearchProductsTool.ExecuteAsync(scoped, gate, bindings, catalogs,
                client, "tea", semanticSearch: semantic, cancellationToken: token),
            "recommendations" => await StorefrontRecommendProductsTool.ExecuteAsync(scoped, gate, bindings,
                catalogs, products, client, "seed", cancellationToken: token),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
    }
}
