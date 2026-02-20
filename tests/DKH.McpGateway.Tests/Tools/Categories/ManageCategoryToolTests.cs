using DKH.McpGateway.Application.Tools.Categories;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Categories;

public class ManageCategoryToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly CategoryManagementService.CategoryManagementServiceClient _client =
        Substitute.For<CategoryManagementService.CategoryManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsCreatedCategoryAsync()
    {
        _client.CreateAsync(
                Arg.Any<CreateCategoryRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CategoryModel { Code = "electronics" }));

        var result = await ManageCategoryTool.ExecuteAsync(
            _auth, _client, "create",
            json: /*lang=json,strict*/ """{"code":"electronics","translations":[{"languageCode":"en","name":"Electronics"}]}""");

        result.Should().Contain("electronics");
    }

    [Fact]
    public async Task Create_MissingJson_ReturnsErrorAsync()
    {
        var result = await ManageCategoryTool.ExecuteAsync(_auth, _client, "create");

        Parse(result).GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task Delete_MissingCode_ReturnsErrorAsync()
    {
        var result = await ManageCategoryTool.ExecuteAsync(_auth, _client, "delete");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task Get_MissingCode_ReturnsErrorAsync()
    {
        var result = await ManageCategoryTool.ExecuteAsync(_auth, _client, "get");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsListAsync()
    {
        _client.ListAsync(
                Arg.Any<ListCategoriesRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListCategoriesResponse
            {
                Items = { new CategoryModel { Code = "cat-1" } },
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
            }));

        var result = await ManageCategoryTool.ExecuteAsync(_auth, _client, "list");

        Parse(result).GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageCategoryTool.ExecuteAsync(_auth, _client, "merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Create_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageCategoryTool.ExecuteAsync(ApiKeyContextMocks.ReadOnly(), _client, "create", json: "{}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
