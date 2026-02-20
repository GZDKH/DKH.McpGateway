using DKH.McpGateway.Application.Tools.Variants;
using Google.Protobuf.WellKnownTypes;
using VarAttr = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantAttrManagement.v1;
using VarVal = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantAttrValueManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Variants;

public class ManageVariantAttrToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly VarAttr.VariantAttrManagementService.VariantAttrManagementServiceClient _client =
        Substitute.For<VarAttr.VariantAttrManagementService.VariantAttrManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new VarAttr.VariantAttrModel
        {
            ProductCode = "prod-1",
            ProductAttributeCode = "pa-size",
            ProductAttributeName = "Size",
            ControlType = "dropdown",
            DisplayOrder = 1,
        };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"productCode\":\"prod-1\",\"productAttributeCode\":\"pa-size\"}");

        result.Should().Contain("prod-1");
        result.Should().Contain("pa-size");
    }

    [Fact]
    public async Task Create_MissingJson_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("create", json: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsModelAsync()
    {
        var model = new VarAttr.VariantAttrModel { ProductCode = "prod-1", ProductAttributeCode = "pa-size" };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "va-1");

        result.Should().Contain("prod-1");
    }

    [Fact]
    public async Task Get_MissingCode_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get", code: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsItemsAsync()
    {
        var response = new VarAttr.ListVariantAttrsResponse { TotalCount = 2, Page = 1, PageSize = 20 };
        response.Items.Add(new VarAttr.VariantAttrModel { ProductCode = "prod-1", ProductAttributeCode = "pa-size" });
        response.Items.Add(new VarAttr.VariantAttrModel { ProductCode = "prod-1", ProductAttributeCode = "pa-color" });
        SetupList(response);

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(2);
        json.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsDeletedMessageAsync()
    {
        SetupDelete();

        var result = await ExecuteToolAsync("delete", code: "va-1");

        result.Should().Contain("Deleted: va-1");
    }

    [Fact]
    public async Task Delete_MissingCode_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("delete", code: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("unknown");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Theory]
    [InlineData("CREATE")]
    [InlineData("Get")]
    [InlineData("LIST")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupCreate(new VarAttr.VariantAttrModel { ProductCode = "prod-1" });
        SetupGet(new VarAttr.VariantAttrModel { ProductCode = "prod-1" });
        SetupList(new VarAttr.ListVariantAttrsResponse { TotalCount = 0, Page = 1, PageSize = 20 });

        var result = await ExecuteToolAsync(action,
            json: action.StartsWith("C", StringComparison.OrdinalIgnoreCase)
                ? /*lang=json,strict*/ "{\"productCode\":\"prod-1\"}"
                : null,
            code: action.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? null : "va-1");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PermissionDenied_Create_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageVariantAttrTool.ExecuteAsync(
            readOnly, _client, action: "create",
            json: /*lang=json,strict*/ "{\"productCode\":\"prod-1\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<VarAttr.ListVariantAttrsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<VarAttr.ListVariantAttrsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null)
        => ManageVariantAttrTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize);

    private void SetupCreate(VarAttr.VariantAttrModel response)
        => _client.CreateAsync(
                Arg.Any<VarAttr.CreateVariantAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(VarAttr.VariantAttrModel response)
        => _client.GetAsync(
                Arg.Any<VarAttr.GetVariantAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(VarAttr.ListVariantAttrsResponse response)
        => _client.ListAsync(
                Arg.Any<VarAttr.ListVariantAttrsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<VarAttr.DeleteVariantAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageVariantAttrValueToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly VarVal.VariantAttrValueManagementService.VariantAttrValueManagementServiceClient _client =
        Substitute.For<VarVal.VariantAttrValueManagementService.VariantAttrValueManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new VarVal.VariantAttrValueModel
        {
            ProductCode = "prod-1",
            ProductAttributeCode = "pa-size",
            ProductAttributeOptionCode = "opt-lg",
            PriceAdjustment = 5.0,
        };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"productCode\":\"prod-1\",\"productAttributeOptionCode\":\"opt-lg\"}");

        result.Should().Contain("prod-1");
        result.Should().Contain("opt-lg");
    }

    [Fact]
    public async Task Create_MissingJson_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("create", json: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsModelAsync()
    {
        var model = new VarVal.VariantAttrValueModel
        {
            ProductCode = "prod-1",
            ProductAttributeOptionCode = "opt-lg",
            Quantity = 10,
        };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "vav-1");

        result.Should().Contain("prod-1");
    }

    [Fact]
    public async Task Get_MissingCode_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get", code: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsItemsAsync()
    {
        var response = new VarVal.ListVariantAttrValuesResponse { TotalCount = 1, Page = 1, PageSize = 20 };
        response.Items.Add(new VarVal.VariantAttrValueModel { ProductCode = "prod-1" });
        SetupList(response);

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsDeletedMessageAsync()
    {
        SetupDelete();

        var result = await ExecuteToolAsync("delete", code: "vav-1");

        result.Should().Contain("Deleted: vav-1");
    }

    [Fact]
    public async Task Delete_MissingCode_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("delete", code: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("unknown");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task PermissionDenied_Delete_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageVariantAttrValueTool.ExecuteAsync(
            readOnly, _client, action: "delete", code: "vav-1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetAsync(
                Arg.Any<VarVal.GetVariantAttrValueRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<VarVal.VariantAttrValueModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("get", code: "vav-1");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null)
        => ManageVariantAttrValueTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize);

    private void SetupCreate(VarVal.VariantAttrValueModel response)
        => _client.CreateAsync(
                Arg.Any<VarVal.CreateVariantAttrValueRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(VarVal.VariantAttrValueModel response)
        => _client.GetAsync(
                Arg.Any<VarVal.GetVariantAttrValueRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(VarVal.ListVariantAttrValuesResponse response)
        => _client.ListAsync(
                Arg.Any<VarVal.ListVariantAttrValuesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<VarVal.DeleteVariantAttrValueRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
