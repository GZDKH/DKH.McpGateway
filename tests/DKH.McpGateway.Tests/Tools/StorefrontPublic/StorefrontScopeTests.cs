using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Application.Tools.StorefrontPublic;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Catalog.v1;

namespace DKH.McpGateway.Tests.Tools.StorefrontPublic;

public class StorefrontScopeTests
{
    private static readonly Guid StorefrontA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly IStorefrontMcpGate _gate = Substitute.For<IStorefrontMcpGate>();

    private readonly StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient _storefrontCatalogClient =
        Substitute.For<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();

    private readonly CatalogManagementService.CatalogManagementServiceClient _catalogClient =
        Substitute.For<CatalogManagementService.CatalogManagementServiceClient>();

    private static IApiKeyContext StorefrontKey(Guid? storefrontId)
    {
        var ctx = Substitute.For<IApiKeyContext>();
        ctx.Scope.Returns(ApiKeyScope.Storefront);
        ctx.StorefrontId.Returns(storefrontId);
        return ctx;
    }

    [Fact]
    public void RequireStorefrontId_WhenScopeNotStorefront_ThrowsAsync()
    {
        var ctx = Substitute.For<IApiKeyContext>();
        ctx.Scope.Returns(ApiKeyScope.Mcp);
        ctx.StorefrontId.Returns(StorefrontA);

        var act = () => StorefrontScope.RequireStorefrontId(ctx);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void RequireStorefrontId_WhenStorefrontIdMissing_Throws()
    {
        var act = () => StorefrontScope.RequireStorefrontId(StorefrontKey(storefrontId: null));

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Resolve_ReturnsOnlyBoundStorefrontCatalogs_AndEnforcesGateAsync()
    {
        _storefrontCatalogClient.GetCatalogsAsync(
                Arg.Any<GetCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse
            {
                Catalogs =
                {
                    new StorefrontCatalogModel { CatalogId = new GuidValue("cat-a"), IsVisible = true },
                },
            }));

        _catalogClient.GetCatalogsAsync(
                Arg.Any<GetStorefrontCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontCatalogsResponse
            {
                Catalogs = { new StorefrontCatalogItem { Name = "Catalog A", SeoName = "catalog-a" } },
            }));

        var seoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            StorefrontKey(StorefrontA), _gate, _storefrontCatalogClient, _catalogClient, "en", CancellationToken.None);

        seoNames.Should().ContainSingle().Which.Should().Be("catalog-a");

        // Isolation: the storefront lookup is scoped to the bound storefront id, never a global query.
        _ = _storefrontCatalogClient.Received(1).GetCatalogsAsync(
            Arg.Is<GetCatalogsRequest>(r => r.StorefrontId.Value == StorefrontA.ToString()),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());

        // The McpEnabled gate is always enforced before any data is read.
        await _gate.Received(1).EnsureMcpEnabledAsync(StorefrontA, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_WhenNoVisibleCatalogs_ReturnsEmpty_AndDoesNotQueryProductCatalogAsync()
    {
        _storefrontCatalogClient.GetCatalogsAsync(
                Arg.Any<GetCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse()));

        var seoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            StorefrontKey(StorefrontA), _gate, _storefrontCatalogClient, _catalogClient, "en", CancellationToken.None);

        seoNames.Should().BeEmpty();

        // No catalogs → never falls back to an unscoped ProductCatalog lookup.
        _ = _catalogClient.DidNotReceive().GetCatalogsAsync(
            Arg.Any<GetStorefrontCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }
}
