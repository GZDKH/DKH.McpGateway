using DKH.McpGateway.Application.Tools.ProductAttributes;
using Google.Protobuf.WellKnownTypes;
using PaGroup = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrGroupManagement.v1;
using PaAttr = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrManagement.v1;
using PaOpt = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrOptionManagement.v1;

namespace DKH.McpGateway.Tests.Tools.ProductAttributes;

public class ManageProductAttrGroupToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly PaGroup.ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient _client =
        Substitute.For<PaGroup.ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new PaGroup.ProductAttrGroupModel
        {
            Code = "pag-general",
            IconName = "settings",
            DisplayOrder = 1,
            Published = true,
        };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"code\":\"pag-general\",\"iconName\":\"settings\"}");

        result.Should().Contain("pag-general");
        result.Should().Contain("settings");
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
        var model = new PaGroup.ProductAttrGroupModel { Code = "pag-general", Published = true };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "pag-general");

        result.Should().Contain("pag-general");
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
        var response = new PaGroup.ListProductAttrGroupsResponse { TotalCount = 2, Page = 1, PageSize = 20 };
        response.Items.Add(new PaGroup.ProductAttrGroupModel { Code = "pag-general" });
        response.Items.Add(new PaGroup.ProductAttrGroupModel { Code = "pag-advanced" });
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

        var result = await ExecuteToolAsync("delete", code: "pag-general");

        result.Should().Contain("Deleted: pag-general");
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
    [InlineData("Delete")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupCreate(new PaGroup.ProductAttrGroupModel { Code = "pag-1" });
        SetupGet(new PaGroup.ProductAttrGroupModel { Code = "pag-1" });
        SetupList(new PaGroup.ListProductAttrGroupsResponse { TotalCount = 0, Page = 1, PageSize = 20 });
        SetupDelete();

        var result = await ExecuteToolAsync(action,
            json: action.StartsWith("C", StringComparison.OrdinalIgnoreCase)
                ? /*lang=json,strict*/ "{\"code\":\"pag-1\"}"
                : null,
            code: action.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? null : "pag-1");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PermissionDenied_Create_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageProductAttrGroupTool.ExecuteAsync(
            readOnly, _client, action: "create",
            json: /*lang=json,strict*/ "{\"code\":\"pag-1\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task PermissionDenied_Delete_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageProductAttrGroupTool.ExecuteAsync(
            readOnly, _client, action: "delete", code: "pag-1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<PaGroup.ListProductAttrGroupsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<PaGroup.ListProductAttrGroupsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageProductAttrGroupTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(PaGroup.ProductAttrGroupModel response)
        => _client.CreateAsync(
                Arg.Any<PaGroup.CreateProductAttrGroupRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(PaGroup.ProductAttrGroupModel response)
        => _client.GetAsync(
                Arg.Any<PaGroup.GetProductAttrGroupRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(PaGroup.ListProductAttrGroupsResponse response)
        => _client.ListAsync(
                Arg.Any<PaGroup.ListProductAttrGroupsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<PaGroup.DeleteProductAttrGroupRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageProductAttrToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly PaAttr.ProductAttrManagementService.ProductAttrManagementServiceClient _client =
        Substitute.For<PaAttr.ProductAttrManagementService.ProductAttrManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new PaAttr.ProductAttrModel
        {
            Code = "pa-size",
            GroupCode = "pag-general",
            Published = true,
            IsRequired = true,
        };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"code\":\"pa-size\",\"groupCode\":\"pag-general\"}");

        result.Should().Contain("pa-size");
        result.Should().Contain("pag-general");
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
        var model = new PaAttr.ProductAttrModel { Code = "pa-size", GroupCode = "pag-general" };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "pa-size");

        result.Should().Contain("pa-size");
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
        var response = new PaAttr.ListProductAttrsResponse { TotalCount = 1, Page = 1, PageSize = 20 };
        response.Items.Add(new PaAttr.ProductAttrModel { Code = "pa-size" });
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

        var result = await ExecuteToolAsync("delete", code: "pa-size");

        result.Should().Contain("Deleted: pa-size");
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
    public async Task PermissionDenied_Create_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageProductAttrTool.ExecuteAsync(
            readOnly, _client, action: "create",
            json: /*lang=json,strict*/ "{\"code\":\"pa-1\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetAsync(
                Arg.Any<PaAttr.GetProductAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<PaAttr.ProductAttrModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("get", code: "pa-1");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageProductAttrTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(PaAttr.ProductAttrModel response)
        => _client.CreateAsync(
                Arg.Any<PaAttr.CreateProductAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(PaAttr.ProductAttrModel response)
        => _client.GetAsync(
                Arg.Any<PaAttr.GetProductAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(PaAttr.ListProductAttrsResponse response)
        => _client.ListAsync(
                Arg.Any<PaAttr.ListProductAttrsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<PaAttr.DeleteProductAttrRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageProductAttrOptionToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly PaOpt.ProductAttrOptionManagementService.ProductAttrOptionManagementServiceClient _client =
        Substitute.For<PaOpt.ProductAttrOptionManagementService.ProductAttrOptionManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new PaOpt.ProductAttrOptionModel
        {
            Code = "pao-large",
            ProductAttributeCode = "pa-size",
            DisplayOrder = 1,
            PriceAdjustment = 10.5,
        };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"code\":\"pao-large\",\"productAttributeCode\":\"pa-size\"}");

        result.Should().Contain("pao-large");
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
        var model = new PaOpt.ProductAttrOptionModel { Code = "pao-large", IsPreselected = true };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "pao-large");

        result.Should().Contain("pao-large");
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
        var response = new PaOpt.ListProductAttrOptionsResponse { TotalCount = 3, Page = 1, PageSize = 20 };
        response.Items.Add(new PaOpt.ProductAttrOptionModel { Code = "pao-small" });
        response.Items.Add(new PaOpt.ProductAttrOptionModel { Code = "pao-medium" });
        response.Items.Add(new PaOpt.ProductAttrOptionModel { Code = "pao-large" });
        SetupList(response);

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(3);
        json.GetProperty("items").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsDeletedMessageAsync()
    {
        SetupDelete();

        var result = await ExecuteToolAsync("delete", code: "pao-large");

        result.Should().Contain("Deleted: pao-large");
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
    public async Task PermissionDenied_Update_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageProductAttrOptionTool.ExecuteAsync(
            readOnly, _client, action: "update",
            json: /*lang=json,strict*/ "{\"code\":\"pao-1\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<PaOpt.ListProductAttrOptionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<PaOpt.ListProductAttrOptionsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageProductAttrOptionTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(PaOpt.ProductAttrOptionModel response)
        => _client.CreateAsync(
                Arg.Any<PaOpt.CreateProductAttrOptionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(PaOpt.ProductAttrOptionModel response)
        => _client.GetAsync(
                Arg.Any<PaOpt.GetProductAttrOptionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(PaOpt.ListProductAttrOptionsResponse response)
        => _client.ListAsync(
                Arg.Any<PaOpt.ListProductAttrOptionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<PaOpt.DeleteProductAttrOptionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
