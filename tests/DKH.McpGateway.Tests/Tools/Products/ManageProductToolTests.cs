using DKH.McpGateway.Application.Tools.Products;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Products;

public class ManageProductToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly ProductManagementService.ProductManagementServiceClient _client =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsCreatedProductAsync()
    {
        _client.CreateAsync(
                Arg.Any<CreateProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "new-product", Sku = "SKU-001" }));

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ """{"data":{"code":"new-product","sku":"SKU-001","translations":[{"languageCode":"en","name":"New Product"}]}}""");

        result.Should().Contain("new-product");
    }

    [Fact]
    public async Task Create_MissingJson_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("create");

        Parse(result).GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task Update_HappyPath_ReturnsUpdatedProductAsync()
    {
        _client.UpdateAsync(
                Arg.Any<UpdateProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "updated-product" }));

        var result = await ExecuteToolAsync("update",
            json: /*lang=json,strict*/ """{"data":{"code":"updated-product","translations":[{"languageCode":"en","name":"Updated"}]}}""");

        result.Should().Contain("updated-product");
    }

    [Fact]
    public async Task Update_MissingJson_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("update");

        Parse(result).GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsDeletedAsync()
    {
        _client.DeleteAsync(
                Arg.Any<DeleteProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Google.Protobuf.WellKnownTypes.Empty()));

        var result = await ExecuteToolAsync("delete", code: "test-product");

        result.Should().Contain("test-product");
    }

    [Fact]
    public async Task Delete_MissingCode_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("delete");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsProductAsync()
    {
        _client.GetAsync(
                Arg.Any<GetProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "existing-product" }));

        var result = await ExecuteToolAsync("get", code: "existing-product");

        result.Should().Contain("existing-product");
    }

    [Fact]
    public async Task Get_MissingCode_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task Get_SetsLanguageAsync()
    {
        _client.GetAsync(
                Arg.Any<GetProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "prod" }));

        await ExecuteToolAsync("get", code: "prod", language: "en");

        _ = _client.Received(1).GetAsync(
            Arg.Is<GetProductRequest>(r => r.Code == "prod" && r.Language == "en"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_HappyPath_ReturnsListAsync()
    {
        _client.ListAsync(
                Arg.Any<ListProductsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListProductsResponse
            {
                Items = { new ProductModel { Code = "prod-1" } },
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
            }));

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task List_WithPagination_SetsPageParamsAsync()
    {
        _client.ListAsync(
                Arg.Any<ListProductsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListProductsResponse
            {
                TotalCount = 0,
                Page = 3,
                PageSize = 10,
            }));

        await ExecuteToolAsync("list", page: 3, pageSize: 10, search: "widget");

        _ = _client.Received(1).ListAsync(
            Arg.Is<ListProductsRequest>(r =>
                r.Page == 3 &&
                r.PageSize == 10 &&
                r.Search == "widget"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Theory]
    [InlineData("CREATE")]
    [InlineData("Create")]
    [InlineData("UPDATE")]
    [InlineData("Update")]
    [InlineData("DELETE")]
    [InlineData("Delete")]
    [InlineData("GET")]
    [InlineData("Get")]
    [InlineData("LIST")]
    [InlineData("List")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupAllActions();

        string? code = null;
        string? json = null;
        if (action.Equals("create", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("update", StringComparison.OrdinalIgnoreCase))
        {
            json = /*lang=json,strict*/ """{"data":{"code":"p1"}}""";
        }
        else if (action.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
                 action.Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            code = "p1";
        }

        var act = () => ExecuteToolAsync(action, json: json, code: code);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Create_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageProductTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "create",
            json: /*lang=json,strict*/ """{"data":{"code":"p1"}}""");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Delete_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageProductTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "delete", code: "p1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageProductTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, "get", code: "p1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<ListProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListProductsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task GrpcDeadlineExceeded_ThrowsRpcExceptionAsync()
    {
        _client.CreateAsync(
                Arg.Any<CreateProductRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ProductModel>(
                StatusCode.DeadlineExceeded, "Deadline exceeded"));

        var act = () => ExecuteToolAsync("create",
            json: /*lang=json,strict*/ """{"data":{"code":"p1"}}""");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.DeadlineExceeded);
    }

    private Task<string> ExecuteToolAsync(
        string action,
        string? json = null,
        string? code = null,
        string? search = null,
        int? page = null,
        int? pageSize = null,
        string? language = null)
        => ManageProductTool.ExecuteAsync(
            _auth, _client,
            action: action,
            json: json,
            code: code,
            search: search,
            page: page,
            pageSize: pageSize,
            language: language);

    private void SetupAllActions()
    {
        _client.CreateAsync(
                Arg.Any<CreateProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "p1" }));
        _client.UpdateAsync(
                Arg.Any<UpdateProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "p1" }));
        _client.DeleteAsync(
                Arg.Any<DeleteProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Google.Protobuf.WellKnownTypes.Empty()));
        _client.GetAsync(
                Arg.Any<GetProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ProductModel { Code = "p1" }));
        _client.ListAsync(
                Arg.Any<ListProductsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListProductsResponse
            {
                Items = { new ProductModel { Code = "p1" } },
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
            }));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
