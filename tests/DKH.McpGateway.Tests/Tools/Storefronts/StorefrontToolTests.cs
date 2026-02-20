using DKH.McpGateway.Application.Tools.Storefronts;
using DKH.Platform.Grpc.Common.Types;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontChannelManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontDomainManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Branding.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Catalog.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Channel.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Domain.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;

namespace DKH.McpGateway.Tests.Tools.Storefronts;

#region GetStorefrontTool

public class GetStorefrontToolTests
{
    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _client =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    [Fact]
    public async Task Get_ByCode_ReturnsStorefrontAsync()
    {
        _client.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = "my-store",
                    Name = "My Store",
                    Status = StorefrontStatus.Active,
                    Features = new StorefrontFeaturesModel { CartEnabled = true },
                },
            }));

        var result = await GetStorefrontTool.ExecuteAsync(_client, storefrontCode: "my-store");

        var json = Parse(result);
        json.GetProperty("code").GetString().Should().Be("my-store");
    }

    [Fact]
    public async Task Get_ById_ReturnsStorefrontAsync()
    {
        var id = Guid.NewGuid().ToString();
        _client.GetAsync(
                Arg.Any<GetStorefrontRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(id),
                    Code = "my-store",
                    Name = "My Store",
                    Status = StorefrontStatus.Active,
                },
            }));

        var result = await GetStorefrontTool.ExecuteAsync(_client, storefrontId: id);

        var json = Parse(result);
        json.GetProperty("code").GetString().Should().Be("my-store");
    }

    [Fact]
    public async Task Get_NoParams_ReturnsErrorAsync()
    {
        var result = await GetStorefrontTool.ExecuteAsync(_client);

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontId or storefrontCode");
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

#endregion

#region ManageStorefrontBrandingTool

public class ManageStorefrontBrandingToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _crudClient =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    private readonly StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient _brandingClient =
        Substitute.For<StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient>();

    public ManageStorefrontBrandingToolTests()
    {
        SetupGetByCode("my-store");
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsBrandingAsync()
    {
        _brandingClient.GetBrandingAsync(
                Arg.Any<GetBrandingRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetBrandingResponse
            {
                Branding = new StorefrontBrandingModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    StorefrontId = new GuidValue(Guid.NewGuid().ToString()),
                    Colors = new ThemeColorsModel { Primary = "#2563eb" },
                },
            }));

        var result = await ManageStorefrontBrandingTool.ExecuteAsync(
            _auth, _crudClient, _brandingClient, "my-store", "get");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Update_HappyPath_ReturnsUpdatedAsync()
    {
        _brandingClient.UpdateBrandingAsync(
                Arg.Any<UpdateBrandingRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new UpdateBrandingResponse
            {
                Branding = new StorefrontBrandingModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    StorefrontId = new GuidValue(Guid.NewGuid().ToString()),
                    Colors = new ThemeColorsModel { Primary = "#ff0000" },
                },
            }));

        var result = await ManageStorefrontBrandingTool.ExecuteAsync(
            _auth, _crudClient, _brandingClient, "my-store", "update", primaryColor: "#ff0000");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("updated");
    }

    [Fact]
    public async Task Reset_HappyPath_ReturnsResetAsync()
    {
        _brandingClient.ResetToDefaultAsync(
                Arg.Any<ResetBrandingRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ResetBrandingResponse
            {
                Branding = new StorefrontBrandingModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    StorefrontId = new GuidValue(Guid.NewGuid().ToString()),
                },
            }));

        var result = await ManageStorefrontBrandingTool.ExecuteAsync(
            _auth, _crudClient, _brandingClient, "my-store", "reset");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("reset");
    }

    [Fact]
    public async Task NotFound_ReturnsErrorAsync()
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse()));

        var result = await ManageStorefrontBrandingTool.ExecuteAsync(
            _auth, _crudClient, _brandingClient, "nonexistent", "get");

        Parse(result).GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontBrandingTool.ExecuteAsync(
            _auth, _crudClient, _brandingClient, "my-store", "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    private void SetupGetByCode(string code)
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = code,
                    Name = "Test Store",
                    Status = StorefrontStatus.Active,
                },
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

#endregion

#region ManageStorefrontCatalogsTool

public class ManageStorefrontCatalogsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _crudClient =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    private readonly StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient _catalogClient =
        Substitute.For<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();

    public ManageStorefrontCatalogsToolTests()
    {
        SetupGetByCode("my-store");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsCatalogsAsync()
    {
        _catalogClient.GetCatalogsAsync(
                Arg.Any<GetCatalogsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse
            {
                Catalogs =
                {
                    new StorefrontCatalogModel
                    {
                        Id = new GuidValue(Guid.NewGuid().ToString()),
                        CatalogId = new GuidValue(Guid.NewGuid().ToString()),
                        DisplayOrder = 1,
                        IsDefault = true,
                        IsVisible = true,
                    },
                },
            }));

        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "list");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("catalogs").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Add_HappyPath_ReturnsAddedAsync()
    {
        var catalogId = Guid.NewGuid().ToString();

        _catalogClient.AddCatalogAsync(
                Arg.Any<AddCatalogRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new AddCatalogResponse
            {
                Catalog = new StorefrontCatalogModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    CatalogId = new GuidValue(catalogId),
                    IsDefault = false,
                    IsVisible = true,
                },
            }));

        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "add", catalogId: catalogId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("added");
    }

    [Fact]
    public async Task Add_MissingCatalogId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "add");

        Parse(result).GetProperty("error").GetString().Should().Contain("catalogId is required");
    }

    [Fact]
    public async Task Remove_HappyPath_ReturnsRemovedAsync()
    {
        var linkId = Guid.NewGuid().ToString();

        _catalogClient.RemoveCatalogAsync(
                Arg.Any<RemoveCatalogRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new RemoveCatalogResponse { Success = true }));

        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "remove", catalogLinkId: linkId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("removed");
    }

    [Fact]
    public async Task Remove_MissingLinkId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "remove");

        Parse(result).GetProperty("error").GetString().Should().Contain("catalogLinkId is required");
    }

    [Fact]
    public async Task SetDefault_HappyPath_ReturnsSetAsync()
    {
        var linkId = Guid.NewGuid().ToString();

        _catalogClient.SetDefaultCatalogAsync(
                Arg.Any<SetDefaultCatalogRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SetDefaultCatalogResponse
            {
                Catalog = new StorefrontCatalogModel
                {
                    Id = new GuidValue(linkId),
                    CatalogId = new GuidValue(Guid.NewGuid().ToString()),
                    IsDefault = true,
                },
            }));

        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "set_default", catalogLinkId: linkId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("set_default");
    }

    [Fact]
    public async Task SetDefault_MissingLinkId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "set_default");

        Parse(result).GetProperty("error").GetString().Should().Contain("catalogLinkId is required");
    }

    [Fact]
    public async Task NotFound_ReturnsErrorAsync()
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse()));

        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "nonexistent", "list");

        Parse(result).GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontCatalogsTool.ExecuteAsync(
            _auth, _crudClient, _catalogClient, "my-store", "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    private void SetupGetByCode(string code)
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = code,
                    Name = "Test Store",
                    Status = StorefrontStatus.Active,
                },
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

#endregion

#region ManageStorefrontDomainsTool

public class ManageStorefrontDomainsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _crudClient =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    private readonly StorefrontDomainManagementService.StorefrontDomainManagementServiceClient _domainClient =
        Substitute.For<StorefrontDomainManagementService.StorefrontDomainManagementServiceClient>();

    public ManageStorefrontDomainsToolTests()
    {
        SetupGetByCode("my-store");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsDomainsAsync()
    {
        _domainClient.GetDomainsAsync(
                Arg.Any<GetDomainsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetDomainsResponse
            {
                Domains =
                {
                    new StorefrontDomainModel
                    {
                        Id = new GuidValue(Guid.NewGuid().ToString()),
                        StorefrontId = new GuidValue(Guid.NewGuid().ToString()),
                        Domain = "shop.example.com",
                        IsPrimary = true,
                        IsVerified = true,
                        VerificationToken = "abc123",
                        SslStatus = SslStatus.Active,
                    },
                },
            }));

        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "list");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("domains").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Add_HappyPath_ReturnsAddedAsync()
    {
        _domainClient.AddDomainAsync(
                Arg.Any<AddDomainRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new AddDomainResponse
            {
                Domain = new StorefrontDomainModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    StorefrontId = new GuidValue(Guid.NewGuid().ToString()),
                    Domain = "shop.example.com",
                    IsPrimary = false,
                    VerificationToken = "verify-123",
                    SslStatus = SslStatus.Pending,
                },
            }));

        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "add", domain: "shop.example.com");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("added");
    }

    [Fact]
    public async Task Add_MissingDomain_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "add");

        Parse(result).GetProperty("error").GetString().Should().Contain("domain is required");
    }

    [Fact]
    public async Task Remove_HappyPath_ReturnsRemovedAsync()
    {
        var domainId = Guid.NewGuid().ToString();

        _domainClient.RemoveDomainAsync(
                Arg.Any<RemoveDomainRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new RemoveDomainResponse { Success = true }));

        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "remove", domainId: domainId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("removed");
    }

    [Fact]
    public async Task Remove_MissingDomainId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "remove");

        Parse(result).GetProperty("error").GetString().Should().Contain("domainId is required");
    }

    [Fact]
    public async Task Verify_HappyPath_ReturnsVerifiedAsync()
    {
        var domainId = Guid.NewGuid().ToString();

        _domainClient.VerifyDomainAsync(
                Arg.Any<VerifyDomainRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new VerifyDomainResponse
            {
                Domain = new StorefrontDomainModel
                {
                    Id = new GuidValue(domainId),
                    Domain = "shop.example.com",
                    IsVerified = true,
                },
                IsVerified = true,
            }));

        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "verify", domainId: domainId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("isVerified").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Verify_MissingDomainId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "verify");

        Parse(result).GetProperty("error").GetString().Should().Contain("domainId is required");
    }

    [Fact]
    public async Task SetPrimary_HappyPath_ReturnsSetAsync()
    {
        var domainId = Guid.NewGuid().ToString();

        _domainClient.SetPrimaryAsync(
                Arg.Any<SetPrimaryDomainRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SetPrimaryDomainResponse
            {
                Domain = new StorefrontDomainModel
                {
                    Id = new GuidValue(domainId),
                    Domain = "shop.example.com",
                    IsPrimary = true,
                },
            }));

        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "set_primary", domainId: domainId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("set_primary");
    }

    [Fact]
    public async Task SetPrimary_MissingDomainId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "set_primary");

        Parse(result).GetProperty("error").GetString().Should().Contain("domainId is required");
    }

    [Fact]
    public async Task NotFound_ReturnsErrorAsync()
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse()));

        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "nonexistent", "list");

        Parse(result).GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontDomainsTool.ExecuteAsync(
            _auth, _crudClient, _domainClient, "my-store", "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    private void SetupGetByCode(string code)
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = code,
                    Name = "Test Store",
                    Status = StorefrontStatus.Active,
                },
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

#endregion

#region ManageStorefrontFeaturesTool

public class ManageStorefrontFeaturesToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _crudClient =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    private readonly StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient _featuresClient =
        Substitute.For<StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient>();

    public ManageStorefrontFeaturesToolTests()
    {
        SetupGetByCode("my-store");
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsFeaturesAsync()
    {
        _featuresClient.GetFeaturesAsync(
                Arg.Any<GetFeaturesRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetFeaturesResponse
            {
                Features = new StorefrontFeaturesModel
                {
                    CartEnabled = true,
                    OrdersEnabled = true,
                    PaymentsEnabled = false,
                    ReviewsEnabled = true,
                    WishlistEnabled = false,
                },
            }));

        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "get");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("features").GetProperty("cartEnabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Enable_HappyPath_ReturnsEnabledAsync()
    {
        _featuresClient.EnableFeatureAsync(
                Arg.Any<EnableFeatureRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new EnableFeatureResponse
            {
                Features = new StorefrontFeaturesModel { CartEnabled = true },
            }));

        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "enable", featureName: "cart");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("enabled");
    }

    [Fact]
    public async Task Enable_MissingFeatureName_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "enable");

        Parse(result).GetProperty("error").GetString().Should().Contain("featureName is required");
    }

    [Fact]
    public async Task Disable_HappyPath_ReturnsDisabledAsync()
    {
        _featuresClient.DisableFeatureAsync(
                Arg.Any<DisableFeatureRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new DisableFeatureResponse
            {
                Features = new StorefrontFeaturesModel { CartEnabled = false },
            }));

        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "disable", featureName: "cart");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("disabled");
    }

    [Fact]
    public async Task Disable_MissingFeatureName_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "disable");

        Parse(result).GetProperty("error").GetString().Should().Contain("featureName is required");
    }

    [Fact]
    public async Task Update_HappyPath_ReturnsUpdatedAsync()
    {
        _featuresClient.GetFeaturesAsync(
                Arg.Any<GetFeaturesRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetFeaturesResponse
            {
                Features = new StorefrontFeaturesModel(),
            }));

        _featuresClient.UpdateFeaturesAsync(
                Arg.Any<UpdateFeaturesRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new UpdateFeaturesResponse
            {
                Features = new StorefrontFeaturesModel { CartEnabled = true, OrdersEnabled = true },
            }));

        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "update",
            cartEnabled: true, ordersEnabled: true);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("updated");
    }

    [Fact]
    public async Task NotFound_ReturnsErrorAsync()
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse()));

        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "nonexistent", "get");

        Parse(result).GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontFeaturesTool.ExecuteAsync(
            _auth, _crudClient, _featuresClient, "my-store", "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    private void SetupGetByCode(string code)
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = code,
                    Name = "Test Store",
                    Status = StorefrontStatus.Active,
                },
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

#endregion

#region ManageStorefrontChannelsTool

public class ManageStorefrontChannelsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _crudClient =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    private readonly StorefrontChannelManagementService.StorefrontChannelManagementServiceClient _channelClient =
        Substitute.For<StorefrontChannelManagementService.StorefrontChannelManagementServiceClient>();

    public ManageStorefrontChannelsToolTests()
    {
        SetupGetByCode("my-store");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsChannelsAsync()
    {
        _channelClient.GetChannelsAsync(
                Arg.Any<GetChannelsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetChannelsResponse
            {
                Channels =
                {
                    new StorefrontChannelModel
                    {
                        Id = new GuidValue(Guid.NewGuid().ToString()),
                        ChannelType = ChannelType.Telegram,
                        ExternalId = "test_bot",
                        IsDefault = true,
                        IsActive = true,
                        Purpose = ChannelPurpose.Sales,
                    },
                },
            }));

        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "list");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("channels").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Add_HappyPath_ReturnsAddedAsync()
    {
        _channelClient.AddChannelAsync(
                Arg.Any<AddChannelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new AddChannelResponse
            {
                Channel = new StorefrontChannelModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    ChannelType = ChannelType.Telegram,
                    ExternalId = "test_bot",
                },
            }));

        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "add",
            channelType: "Telegram", externalId: "test_bot");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("added");
    }

    [Fact]
    public async Task Add_MissingChannelType_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "add", externalId: "test_bot");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelType and externalId are required");
    }

    [Fact]
    public async Task Add_MissingExternalId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "add", channelType: "Telegram");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelType and externalId are required");
    }

    [Fact]
    public async Task Update_HappyPath_ReturnsUpdatedAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _channelClient.UpdateChannelAsync(
                Arg.Any<UpdateChannelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new UpdateChannelResponse
            {
                Channel = new StorefrontChannelModel
                {
                    Id = new GuidValue(channelId),
                    IsActive = false,
                },
            }));

        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "update",
            channelId: channelId, isActive: false);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("updated");
    }

    [Fact]
    public async Task Update_MissingChannelId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "update");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelId is required");
    }

    [Fact]
    public async Task Remove_HappyPath_ReturnsRemovedAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _channelClient.RemoveChannelAsync(
                Arg.Any<RemoveChannelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new RemoveChannelResponse { Success = true }));

        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "remove", channelId: channelId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("removed");
    }

    [Fact]
    public async Task Remove_MissingChannelId_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "remove");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelId is required");
    }

    [Fact]
    public async Task NotFound_ReturnsErrorAsync()
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse()));

        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "nonexistent", "list");

        Parse(result).GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontChannelsTool.ExecuteAsync(
            _auth, _crudClient, _channelClient, "my-store", "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    private void SetupGetByCode(string code)
    {
        _crudClient.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = code,
                    Name = "Test Store",
                    Status = StorefrontStatus.Active,
                },
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

#endregion
