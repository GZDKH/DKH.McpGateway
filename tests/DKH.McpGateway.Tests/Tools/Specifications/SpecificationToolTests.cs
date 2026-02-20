using DKH.McpGateway.Application.Tools.Specifications;
using Google.Protobuf.WellKnownTypes;
using SpecGroup = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecGroupManagement.v1;
using SpecAttr = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecAttributeManagement.v1;
using SpecOpt = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecOptionManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Specifications;

public class ManageSpecGroupToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly SpecGroup.SpecGroupManagementService.SpecGroupManagementServiceClient _client =
        Substitute.For<SpecGroup.SpecGroupManagementService.SpecGroupManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new SpecGroup.SpecGroupModel { Code = "sg-color", IconName = "palette", DisplayOrder = 1, Published = true };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create", json: /*lang=json,strict*/ "{\"code\":\"sg-color\"}");

        result.Should().Contain("sg-color");
        result.Should().Contain("palette");
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
        var model = new SpecGroup.SpecGroupModel { Code = "sg-color", Published = true };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "sg-color");

        result.Should().Contain("sg-color");
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
        var response = new SpecGroup.ListSpecGroupsResponse { TotalCount = 2, Page = 1, PageSize = 20 };
        response.Items.Add(new SpecGroup.SpecGroupModel { Code = "sg-1" });
        response.Items.Add(new SpecGroup.SpecGroupModel { Code = "sg-2" });
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

        var result = await ExecuteToolAsync("delete", code: "sg-color");

        result.Should().Contain("Deleted: sg-color");
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
        SetupCreate(new SpecGroup.SpecGroupModel { Code = "sg-1" });
        SetupGet(new SpecGroup.SpecGroupModel { Code = "sg-1" });
        SetupList(new SpecGroup.ListSpecGroupsResponse { TotalCount = 0, Page = 1, PageSize = 20 });
        SetupDelete();

        var result = await ExecuteToolAsync(action,
            json: action.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? /*lang=json,strict*/ "{\"code\":\"sg-1\"}" : null,
            code: action.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? null : "sg-1");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PermissionDenied_Create_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageSpecGroupTool.ExecuteAsync(
            readOnly, _client, action: "create", json: /*lang=json,strict*/ "{\"code\":\"sg-1\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task PermissionDenied_Delete_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageSpecGroupTool.ExecuteAsync(
            readOnly, _client, action: "delete", code: "sg-1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<SpecGroup.ListSpecGroupsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<SpecGroup.ListSpecGroupsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageSpecGroupTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(SpecGroup.SpecGroupModel response)
        => _client.CreateAsync(
                Arg.Any<SpecGroup.CreateSpecGroupRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(SpecGroup.SpecGroupModel response)
        => _client.GetAsync(
                Arg.Any<SpecGroup.GetSpecGroupRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(SpecGroup.ListSpecGroupsResponse response)
        => _client.ListAsync(
                Arg.Any<SpecGroup.ListSpecGroupsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<SpecGroup.DeleteSpecGroupRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageSpecAttributeToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly SpecAttr.SpecAttributeManagementService.SpecAttributeManagementServiceClient _client =
        Substitute.For<SpecAttr.SpecAttributeManagementService.SpecAttributeManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new SpecAttr.SpecAttributeModel { Code = "sa-weight", GroupCode = "sg-1", Published = true };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create", json: /*lang=json,strict*/ "{\"code\":\"sa-weight\"}");

        result.Should().Contain("sa-weight");
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
        var model = new SpecAttr.SpecAttributeModel { Code = "sa-weight", GroupCode = "sg-1" };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "sa-weight");

        result.Should().Contain("sa-weight");
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
        var response = new SpecAttr.ListSpecAttributesResponse { TotalCount = 1, Page = 1, PageSize = 20 };
        response.Items.Add(new SpecAttr.SpecAttributeModel { Code = "sa-weight" });
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

        var result = await ExecuteToolAsync("delete", code: "sa-weight");

        result.Should().Contain("Deleted: sa-weight");
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

        var act = () => ManageSpecAttributeTool.ExecuteAsync(
            readOnly, _client, action: "create", json: /*lang=json,strict*/ "{\"code\":\"sa-1\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetAsync(
                Arg.Any<SpecAttr.GetSpecAttributeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<SpecAttr.SpecAttributeModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("get", code: "sa-1");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageSpecAttributeTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(SpecAttr.SpecAttributeModel response)
        => _client.CreateAsync(
                Arg.Any<SpecAttr.CreateSpecAttributeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(SpecAttr.SpecAttributeModel response)
        => _client.GetAsync(
                Arg.Any<SpecAttr.GetSpecAttributeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(SpecAttr.ListSpecAttributesResponse response)
        => _client.ListAsync(
                Arg.Any<SpecAttr.ListSpecAttributesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<SpecAttr.DeleteSpecAttributeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageSpecOptionToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly SpecOpt.SpecOptionManagementService.SpecOptionManagementServiceClient _client =
        Substitute.For<SpecOpt.SpecOptionManagementService.SpecOptionManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsFormattedModelAsync()
    {
        var model = new SpecOpt.SpecOptionModel { Code = "so-red", SpecificationAttributeCode = "sa-color", Published = true };
        SetupCreate(model);

        var result = await ExecuteToolAsync("create", json: /*lang=json,strict*/ "{\"code\":\"so-red\"}");

        result.Should().Contain("so-red");
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
        var model = new SpecOpt.SpecOptionModel { Code = "so-red", ColorSquaresRgb = "#FF0000" };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "so-red");

        result.Should().Contain("so-red");
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
        var response = new SpecOpt.ListSpecOptionsResponse { TotalCount = 3, Page = 1, PageSize = 20 };
        response.Items.Add(new SpecOpt.SpecOptionModel { Code = "so-red" });
        response.Items.Add(new SpecOpt.SpecOptionModel { Code = "so-green" });
        response.Items.Add(new SpecOpt.SpecOptionModel { Code = "so-blue" });
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

        var result = await ExecuteToolAsync("delete", code: "so-red");

        result.Should().Contain("Deleted: so-red");
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

        var act = () => ManageSpecOptionTool.ExecuteAsync(
            readOnly, _client, action: "delete", code: "so-1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<SpecOpt.ListSpecOptionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<SpecOpt.ListSpecOptionsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageSpecOptionTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(SpecOpt.SpecOptionModel response)
        => _client.CreateAsync(
                Arg.Any<SpecOpt.CreateSpecOptionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(SpecOpt.SpecOptionModel response)
        => _client.GetAsync(
                Arg.Any<SpecOpt.GetSpecOptionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(SpecOpt.ListSpecOptionsResponse response)
        => _client.ListAsync(
                Arg.Any<SpecOpt.ListSpecOptionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<SpecOpt.DeleteSpecOptionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
