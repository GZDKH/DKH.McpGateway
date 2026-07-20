using System.Security.Claims;
using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.McpGateway.Application.Tools.Storefronts;
using DKH.Platform.Grpc.Common.Types;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontChannelManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontDomainManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Tests.Tools.Storefronts;

public class StorefrontWorkspaceScopeTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly Guid _workspaceId = Guid.NewGuid();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _crudClient =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    [Fact]
    public async Task GetByCode_PropagatesSelectedWorkspaceAndReturnsOwnedStorefrontAsync()
    {
        SetupStorefront(_workspaceId);
        var scope = StorefrontWorkspaceScope.Resolve(_auth, CreateAccessor(_workspaceId));

        var storefront = await scope.GetByCodeAsync(_crudClient, "main", CancellationToken.None);

        storefront.Code.Should().Be("main");
        _ = _crudClient.Received(1).GetByCodeAsync(
            Arg.Any<GetStorefrontByCodeRequest>(),
            Arg.Is<Metadata>(metadata => HasSelectedWorkspace(metadata)),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForeignAndMissingStorefronts_AreIndistinguishableAsync()
    {
        var scope = StorefrontWorkspaceScope.Resolve(_auth, CreateAccessor(_workspaceId));
        SetupStorefront(Guid.NewGuid());

        Func<Task> foreign = () => scope.GetByCodeAsync(_crudClient, "main", CancellationToken.None);
        var foreignException = (await foreign.Should().ThrowAsync<RpcException>()).Which;

        _crudClient.ClearReceivedCalls();
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetStorefrontByCodeResponse>(StatusCode.NotFound));

        Func<Task> missing = () => scope.GetByCodeAsync(_crudClient, "main", CancellationToken.None);
        var missingException = (await missing.Should().ThrowAsync<RpcException>()).Which;

        missingException.Status.Should().Be(foreignException.Status);
        missingException.Status.Detail.Should().NotContain(_workspaceId.ToString("D"));
    }

    [Fact]
    public async Task ForeignStorefront_FailsBeforeBrandingCallAsync()
    {
        SetupStorefront(Guid.NewGuid());
        var brandingClient = Substitute.For<StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient>();

        Func<Task> action = () => ManageStorefrontBrandingTool.ExecuteAsync(
            _auth,
            CreateAccessor(_workspaceId),
            _crudClient,
            brandingClient,
            "main",
            "get");

        var exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);
        brandingClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ForeignStorefrontId_FailsBeforeFeatureCallAsync()
    {
        SetupStorefrontById(Guid.NewGuid());
        var featuresClient = Substitute.For<StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient>();

        Func<Task> action = () => GetStorefrontFeaturesTool.ExecuteAsync(
            _auth,
            CreateAccessor(_workspaceId),
            _crudClient,
            featuresClient,
            Guid.NewGuid().ToString("D"));

        var exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);
        featuresClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ForeignStorefrontId_FailsBeforeDirectBrandingCallAsync()
    {
        SetupStorefrontById(Guid.NewGuid());
        var brandingClient = Substitute.For<StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient>();

        Func<Task> action = () => GetStorefrontBrandingTool.ExecuteAsync(
            _auth,
            CreateAccessor(_workspaceId),
            _crudClient,
            brandingClient,
            Guid.NewGuid().ToString("D"));

        var exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);
        brandingClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ForeignStorefrontId_OverviewDoesNotFanOutAsync()
    {
        SetupStorefrontById(Guid.NewGuid());
        var domainClient = Substitute.For<StorefrontDomainManagementService.StorefrontDomainManagementServiceClient>();
        var channelClient = Substitute.For<StorefrontChannelManagementService.StorefrontChannelManagementServiceClient>();
        var catalogClient = Substitute.For<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();

        Func<Task> action = () => StorefrontOverviewTool.ExecuteAsync(
            _auth,
            CreateAccessor(_workspaceId),
            _crudClient,
            domainClient,
            channelClient,
            catalogClient,
            Guid.NewGuid().ToString("D"));

        var exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);
        domainClient.ReceivedCalls().Should().BeEmpty();
        channelClient.ReceivedCalls().Should().BeEmpty();
        catalogClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void MissingHttpContext_FailsBeforeCrudCall()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();

        var action = () => StorefrontWorkspaceScope.Resolve(_auth, accessor);

        action.Should().Throw<UnauthorizedAccessException>();
        _crudClient.ReceivedCalls().Should().BeEmpty();
    }

    private void SetupStorefront(Guid workspaceId)
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString("D")),
                    WorkspaceId = new GuidValue(workspaceId.ToString("D")),
                    Code = "main",
                    Name = "Main",
                },
            }));
    }

    private void SetupStorefrontById(Guid workspaceId)
    {
        _crudClient.GetAsync(
                Arg.Any<GetStorefrontRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString("D")),
                    WorkspaceId = new GuidValue(workspaceId.ToString("D")),
                    Code = "main",
                    Name = "Main",
                },
            }));
    }

    private static IHttpContextAccessor CreateAccessor(Guid workspaceId)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "storefront-workspace-test-user")],
                authenticationType: "Test")),
        };
        context.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            workspaceId.ToString("D");

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private bool HasSelectedWorkspace(Metadata metadata) => metadata.Count(entry =>
            entry.Key == ProductCatalogWorkspaceRequestContext.GrpcWorkspaceIdHeaderName
            && entry.Value == _workspaceId.ToString("D")) == 1;
}
