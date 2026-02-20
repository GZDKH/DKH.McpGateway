using DKH.McpGateway.Application.Tools.Brands;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Brands;

public class ManageBrandToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly BrandManagementService.BrandManagementServiceClient _client =
        Substitute.For<BrandManagementService.BrandManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsCreatedBrandAsync()
    {
        _client.CreateAsync(
                Arg.Any<CreateBrandRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new BrandModel { Code = "test-brand" }));

        var result = await ManageBrandTool.ExecuteAsync(
            _auth, _client, "create",
            json: /*lang=json,strict*/ """{"code":"test-brand","translations":[{"languageCode":"en","name":"Test"}]}""");

        result.Should().Contain("test-brand");
    }

    [Fact]
    public async Task Create_MissingJson_ReturnsErrorAsync()
    {
        var result = await ManageBrandTool.ExecuteAsync(_auth, _client, "create");

        Parse(result).GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsDeletedAsync()
    {
        _client.DeleteAsync(
                Arg.Any<DeleteBrandRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Google.Protobuf.WellKnownTypes.Empty()));

        var result = await ManageBrandTool.ExecuteAsync(_auth, _client, "delete", code: "test-brand");

        result.Should().Contain("test-brand");
    }

    [Fact]
    public async Task Delete_MissingCode_ReturnsErrorAsync()
    {
        var result = await ManageBrandTool.ExecuteAsync(_auth, _client, "delete");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task Get_MissingCode_ReturnsErrorAsync()
    {
        var result = await ManageBrandTool.ExecuteAsync(_auth, _client, "get");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsListAsync()
    {
        _client.ListAsync(
                Arg.Any<ListBrandsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListBrandsResponse
            {
                Items = { new BrandModel { Code = "brand-1" } },
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
            }));

        var result = await ManageBrandTool.ExecuteAsync(_auth, _client, "list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageBrandTool.ExecuteAsync(_auth, _client, "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Create_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageBrandTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "create", json: "{}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageBrandTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, "get", code: "test");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
