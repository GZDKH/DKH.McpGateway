using System.Security.Claims;
using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;
using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.McpGateway.Application.Tools.Geography;
using DKH.McpGateway.Application.Tools.Products;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CityManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.StateProvinceManagement.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Tests.Tools.Products;

/// <summary>
/// #85 Workspace-binding contract for the read-side ProductCatalog tools:
/// every downstream RPC carries exactly the selected server-validated
/// workspace (<c>x-workspace-id</c> metadata via the shared #83 resolver), and
/// a request without a valid workspace fails BEFORE any downstream call —
/// stdio-style (no HttpContext) requests fail closed the same way.
/// </summary>
public class WorkspaceScopedCatalogToolsTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly HttpContextAccessor _httpContextAccessor = new();

    // A REAL HttpContextAccessor stores the context in a process-wide
    // AsyncLocal — assigning null on a second instance would wipe the first.
    // The stdio stand-in is therefore a substitute that just returns null.
    private readonly IHttpContextAccessor _stdioAccessor = Substitute.For<IHttpContextAccessor>();

    private readonly CatalogManagementService.CatalogManagementServiceClient _catalogClient =
        Substitute.For<CatalogManagementService.CatalogManagementServiceClient>();

    private readonly CategoryManagementService.CategoryManagementServiceClient _categoryClient =
        Substitute.For<CategoryManagementService.CategoryManagementServiceClient>();

    private readonly ProductManagementService.ProductManagementServiceClient _productClient =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();

    public WorkspaceScopedCatalogToolsTests()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString("D"))],
            authenticationType: "Test"));
        _httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            _workspaceId.ToString("D");
        _httpContextAccessor.HttpContext = _httpContext;
    }

    [Fact]
    public async Task ListCatalogs_PropagatesSelectedWorkspaceAsync()
    {
        _catalogClient.GetCatalogsAsync(
                Arg.Any<GetStorefrontCatalogsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontCatalogsResponse()));

        await ListCatalogsTool.ExecuteAsync(_auth, _httpContextAccessor, _catalogClient);

        _ = _catalogClient.Received(1).GetCatalogsAsync(
            Arg.Any<GetStorefrontCatalogsRequest>(),
            Arg.Is<Metadata>(m => HasExpectedWorkspaceMetadata(m)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCatalogs_StdioStyle_FailsClosedBeforeRpcAsync()
    {
        var act = () => ListCatalogsTool.ExecuteAsync(_auth, _stdioAccessor, _catalogClient);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _catalogClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ListCategories_PropagatesSelectedWorkspaceAsync()
    {
        _categoryClient.GetCategoryTreeAsync(
                Arg.Any<GetCategoryTreeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CategoryTree()));

        await ListCategoriesTool.ExecuteAsync(_auth, _httpContextAccessor, _categoryClient);

        _ = _categoryClient.Received(1).GetCategoryTreeAsync(
            Arg.Any<GetCategoryTreeRequest>(),
            Arg.Is<Metadata>(m => HasExpectedWorkspaceMetadata(m)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CategoryDistribution_MissingWorkspaceHeader_FailsBeforeRpcAsync()
    {
        _httpContext.Request.Headers.Remove(ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName);

        var act = () => CategoryDistributionTool.ExecuteAsync(_auth, _httpContextAccessor, _categoryClient);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.InvalidArgument);
        _categoryClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ProductStats_PropagatesWorkspaceOnBothRpcsAsync()
    {
        _productClient.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SearchProductsResponse()));
        _categoryClient.GetCategoryTreeAsync(
                Arg.Any<GetCategoryTreeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CategoryTree()));

        await ProductStatsTool.ExecuteAsync(_auth, _httpContextAccessor, _productClient, _categoryClient);

        _ = _productClient.Received(1).SearchProductsAsync(
            Arg.Any<SearchProductsRequest>(),
            Arg.Is<Metadata>(m => HasExpectedWorkspaceMetadata(m)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        _ = _categoryClient.Received(1).GetCategoryTreeAsync(
            Arg.Any<GetCategoryTreeRequest>(),
            Arg.Is<Metadata>(m => HasExpectedWorkspaceMetadata(m)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProductOrigin_FailsClosedWithoutWorkspace_MakesNoCallsAsync()
    {
        var countryClient = Substitute.For<CountryManagementService.CountryManagementServiceClient>();
        var provinceClient = Substitute.For<StateProvinceManagementService.StateProvinceManagementServiceClient>();
        var cityClient = Substitute.For<CityManagementService.CityManagementServiceClient>();

        var act = () => ProductOriginTool.ExecuteAsync(
            _auth, _stdioAccessor, _productClient, countryClient, provinceClient, cityClient, "tea");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _productClient.ReceivedCalls().Should().BeEmpty();
        countryClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GetProduct_ForeignWorkspaceSelection_IsCallerProblemNotHeaderForwardingAsync()
    {
        // The gateway forwards ONLY the normalized selected workspace — any
        // extra client-supplied metadata never reaches the RPC; downstream
        // validates membership. (No-oracle authority lives server-side.)
        var counterpartyClient = Substitute.For<CounterpartyCrudService.CounterpartyCrudServiceClient>();
        _productClient.GetProductDetailAsync(
                Arg.Any<GetProductDetailRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ProductDetail>(StatusCode.PermissionDenied));

        var act = () => GetProductTool.ExecuteAsync(
            _auth, _httpContextAccessor, _productClient, counterpartyClient, "tea");

        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
        counterpartyClient.ReceivedCalls().Should().BeEmpty("authorization failures must not fan out further");
    }

    private bool HasExpectedWorkspaceMetadata(Metadata metadata)
    {
        var entries = metadata
            .Where(entry => entry.Key == "x-workspace-id")
            .Select(entry => entry.Value)
            .ToList();
        return entries.Count == 1 && entries[0] == _workspaceId.ToString("D");
    }
}
