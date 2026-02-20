using DKH.McpGateway.Application.Tools.Storefronts;
using DKH.Platform.Grpc.Common.Types;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;

namespace DKH.McpGateway.Tests.Tools.Storefronts;

public class ManageStorefrontToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _client =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsCreatedStorefrontAsync()
    {
        _client.CreateAsync(
                Arg.Any<CreateStorefrontRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CreateStorefrontResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = "my-store",
                    Name = "My Store",
                    Status = StorefrontStatus.Active,
                },
            }));

        var result = await ManageStorefrontTool.ExecuteAsync(
            _auth, _client, "create", storefrontCode: "my-store", name: "My Store",
            ownerId: Guid.NewGuid().ToString());

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("created");
    }

    [Fact]
    public async Task Create_MissingCode_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontTool.ExecuteAsync(_auth, _client, "create", name: "Test");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontCode and name are required");
    }

    [Fact]
    public async Task Create_MissingName_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontTool.ExecuteAsync(_auth, _client, "create", storefrontCode: "test");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontCode and name are required");
    }

    [Fact]
    public async Task Update_MissingStorefrontCode_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontTool.ExecuteAsync(_auth, _client, "update");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontCode is required");
    }

    [Fact]
    public async Task Delete_MissingStorefrontCode_ReturnsErrorAsync()
    {
        var result = await ManageStorefrontTool.ExecuteAsync(_auth, _client, "delete");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontCode is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        SetupGetByCode("test-store");
        var result = await ManageStorefrontTool.ExecuteAsync(
            _auth, _client, "merge", storefrontCode: "test-store");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStorefrontTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, "create",
            storefrontCode: "test", name: "Test");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Update_HappyPath_ReturnsUpdatedAsync()
    {
        SetupGetByCode("test-store");
        _client.UpdateAsync(
                Arg.Any<UpdateStorefrontRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new UpdateStorefrontResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = "test-store",
                    Name = "Updated Store",
                    Status = StorefrontStatus.Active,
                },
            }));

        var result = await ManageStorefrontTool.ExecuteAsync(
            _auth, _client, "update", storefrontCode: "test-store", name: "Updated Store");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("updated");
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsDeletedAsync()
    {
        SetupGetByCode("test-store");
        _client.DeleteAsync(
                Arg.Any<DeleteStorefrontRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new DeleteStorefrontResponse { Success = true }));

        var result = await ManageStorefrontTool.ExecuteAsync(
            _auth, _client, "delete", storefrontCode: "test-store");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("deleted");
    }

    [Fact]
    public async Task ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStorefrontTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "create",
            storefrontCode: "test", name: "Test");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private void SetupGetByCode(string code)
    {
        _client.GetByCodeAsync(
                Arg.Any<GetStorefrontByCodeRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetStorefrontByCodeResponse
            {
                Storefront = new StorefrontModel
                {
                    Id = new GuidValue(Guid.NewGuid().ToString()),
                    Code = code,
                    Name = "Test Store",
                    Status = StorefrontStatus.Active,
                    Features = new StorefrontFeaturesModel(),
                },
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
