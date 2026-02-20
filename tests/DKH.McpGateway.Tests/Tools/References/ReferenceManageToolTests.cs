using DKH.McpGateway.Application.Tools.References;
using Google.Protobuf.WellKnownTypes;
using CountryMgmt = DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using CurrencyMgmt = DKH.ReferenceService.Contracts.Reference.Api.CurrencyManagement.v1;
using LanguageMgmt = DKH.ReferenceService.Contracts.Reference.Api.LanguageManagement.v1;

namespace DKH.McpGateway.Tests.Tools.References;

public class ManageCountryToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly CountryMgmt.CountryManagementService.CountryManagementServiceClient _client =
        Substitute.For<CountryMgmt.CountryManagementService.CountryManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsOkAsync()
    {
        SetupCreate(new CountryMgmt.CountryModel { TwoLetterCode = "US" });

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"twoLetterCode\":\"US\",\"threeLetterCode\":\"USA\",\"numericCode\":840}");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
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
    public async Task Update_HappyPath_ReturnsOkAsync()
    {
        SetupUpdate(new CountryMgmt.CountryModel { TwoLetterCode = "US" });

        var result = await ExecuteToolAsync("update",
            json: /*lang=json,strict*/ "{\"twoLetterCode\":\"US\",\"published\":true}");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsModelAsync()
    {
        var model = new CountryMgmt.CountryModel
        {
            TwoLetterCode = "US",
            ThreeLetterCode = "USA",
            NumericCode = 840,
            Published = true,
        };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "US");

        result.Should().Contain("US");
        result.Should().Contain("USA");
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
        var response = new CountryMgmt.ListCountriesResponse { TotalCount = 2, Page = 1, PageSize = 20 };
        response.Items.Add(new CountryMgmt.CountryModel { TwoLetterCode = "US" });
        response.Items.Add(new CountryMgmt.CountryModel { TwoLetterCode = "CN" });
        SetupList(response);

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(2);
        json.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsOkAsync()
    {
        SetupDelete();

        var result = await ExecuteToolAsync("delete", code: "US");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
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
        SetupCreate(new CountryMgmt.CountryModel { TwoLetterCode = "US" });
        SetupGet(new CountryMgmt.CountryModel { TwoLetterCode = "US" });
        SetupList(new CountryMgmt.ListCountriesResponse { TotalCount = 0, Page = 1, PageSize = 20 });
        SetupDelete();

        var result = await ExecuteToolAsync(action,
            json: action.StartsWith("C", StringComparison.OrdinalIgnoreCase)
                ? /*lang=json,strict*/ "{\"twoLetterCode\":\"US\"}"
                : null,
            code: action.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? null : "US");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PermissionDenied_Create_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageCountryTool.ExecuteAsync(
            readOnly, _client, action: "create",
            json: /*lang=json,strict*/ "{\"twoLetterCode\":\"US\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task PermissionDenied_Delete_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageCountryTool.ExecuteAsync(
            readOnly, _client, action: "delete", code: "US");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<CountryMgmt.ListCountriesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CountryMgmt.ListCountriesResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task GrpcDeadlineExceeded_ThrowsRpcExceptionAsync()
    {
        _client.GetAsync(
                Arg.Any<CountryMgmt.GetCountryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CountryMgmt.CountryModel>(
                StatusCode.DeadlineExceeded, "Deadline exceeded"));

        var act = () => ExecuteToolAsync("get", code: "US");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.DeadlineExceeded);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageCountryTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(CountryMgmt.CountryModel response)
        => _client.CreateAsync(
                Arg.Any<CountryMgmt.ManageCountryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupUpdate(CountryMgmt.CountryModel response)
        => _client.UpdateAsync(
                Arg.Any<CountryMgmt.ManageCountryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(CountryMgmt.CountryModel response)
        => _client.GetAsync(
                Arg.Any<CountryMgmt.GetCountryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(CountryMgmt.ListCountriesResponse response)
        => _client.ListAsync(
                Arg.Any<CountryMgmt.ListCountriesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<CountryMgmt.DeleteCountryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageCurrencyToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient _client =
        Substitute.For<CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsOkAsync()
    {
        SetupCreate(new CurrencyMgmt.CurrencyModel { Code = "USD" });

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"code\":\"USD\",\"rate\":1.0,\"symbol\":\"$\"}");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
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
        var model = new CurrencyMgmt.CurrencyModel
        {
            Code = "USD",
            Rate = 1.0,
            Symbol = "$",
            IsPrimary = true,
            Published = true,
        };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "USD");

        result.Should().Contain("USD");
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
        var response = new CurrencyMgmt.ListCurrenciesResponse { TotalCount = 2, Page = 1, PageSize = 20 };
        response.Items.Add(new CurrencyMgmt.CurrencyModel { Code = "USD" });
        response.Items.Add(new CurrencyMgmt.CurrencyModel { Code = "EUR" });
        SetupList(response);

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(2);
        json.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsOkAsync()
    {
        SetupDelete();

        var result = await ExecuteToolAsync("delete", code: "USD");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
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

        var act = () => ManageCurrencyTool.ExecuteAsync(
            readOnly, _client, action: "create",
            json: /*lang=json,strict*/ "{\"code\":\"USD\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<CurrencyMgmt.ListCurrenciesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CurrencyMgmt.ListCurrenciesResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageCurrencyTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(CurrencyMgmt.CurrencyModel response)
        => _client.CreateAsync(
                Arg.Any<CurrencyMgmt.ManageCurrencyRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(CurrencyMgmt.CurrencyModel response)
        => _client.GetAsync(
                Arg.Any<CurrencyMgmt.GetCurrencyRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(CurrencyMgmt.ListCurrenciesResponse response)
        => _client.ListAsync(
                Arg.Any<CurrencyMgmt.ListCurrenciesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<CurrencyMgmt.DeleteCurrencyRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ManageLanguageToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly LanguageMgmt.LanguageManagementService.LanguageManagementServiceClient _client =
        Substitute.For<LanguageMgmt.LanguageManagementService.LanguageManagementServiceClient>();

    [Fact]
    public async Task Create_HappyPath_ReturnsOkAsync()
    {
        SetupCreate(new LanguageMgmt.LanguageModel { CultureName = "en" });

        var result = await ExecuteToolAsync("create",
            json: /*lang=json,strict*/ "{\"cultureName\":\"en\",\"nativeName\":\"English\"}");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
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
        var model = new LanguageMgmt.LanguageModel
        {
            CultureName = "en",
            NativeName = "English",
            TwoLetterLanguageName = "en",
            Published = true,
        };
        SetupGet(model);

        var result = await ExecuteToolAsync("get", code: "en");

        result.Should().Contain("en");
        result.Should().Contain("English");
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
        var response = new LanguageMgmt.ListLanguagesResponse { TotalCount = 3, Page = 1, PageSize = 20 };
        response.Items.Add(new LanguageMgmt.LanguageModel { CultureName = "en" });
        response.Items.Add(new LanguageMgmt.LanguageModel { CultureName = "ru" });
        response.Items.Add(new LanguageMgmt.LanguageModel { CultureName = "zh" });
        SetupList(response);

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(3);
        json.GetProperty("items").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task List_WithSearchAndPagination_SetsRequestFieldsAsync()
    {
        SetupList(new LanguageMgmt.ListLanguagesResponse { TotalCount = 0, Page = 2, PageSize = 10 });

        await ExecuteToolAsync("list", search: "english", page: 2, pageSize: 10, language: "en");

        _ = _client.Received(1).ListAsync(
            Arg.Is<LanguageMgmt.ListLanguagesRequest>(r =>
                r.Search == "english" &&
                r.Page == 2 &&
                r.PageSize == 10 &&
                r.Language == "en"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_HappyPath_ReturnsOkAsync()
    {
        SetupDelete();

        var result = await ExecuteToolAsync("delete", code: "en");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
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
        SetupCreate(new LanguageMgmt.LanguageModel { CultureName = "en" });
        SetupGet(new LanguageMgmt.LanguageModel { CultureName = "en" });
        SetupList(new LanguageMgmt.ListLanguagesResponse { TotalCount = 0, Page = 1, PageSize = 20 });
        SetupDelete();

        var result = await ExecuteToolAsync(action,
            json: action.StartsWith("C", StringComparison.OrdinalIgnoreCase)
                ? /*lang=json,strict*/ "{\"cultureName\":\"en\"}"
                : null,
            code: action.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? null : "en");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PermissionDenied_Create_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageLanguageTool.ExecuteAsync(
            readOnly, _client, action: "create",
            json: /*lang=json,strict*/ "{\"cultureName\":\"en\"}");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task PermissionDenied_Delete_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ManageLanguageTool.ExecuteAsync(
            readOnly, _client, action: "delete", code: "en");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetAsync(
                Arg.Any<LanguageMgmt.GetLanguageRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<LanguageMgmt.LanguageModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("get", code: "en");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task GrpcDeadlineExceeded_ThrowsRpcExceptionAsync()
    {
        _client.ListAsync(
                Arg.Any<LanguageMgmt.ListLanguagesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<LanguageMgmt.ListLanguagesResponse>(
                StatusCode.DeadlineExceeded, "Deadline exceeded"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.DeadlineExceeded);
    }

    private Task<string> ExecuteToolAsync(
        string action, string? json = null, string? code = null,
        string? search = null, int? page = null, int? pageSize = null, string? language = null)
        => ManageLanguageTool.ExecuteAsync(
            _auth, _client,
            action: action, json: json, code: code,
            search: search, page: page, pageSize: pageSize, language: language);

    private void SetupCreate(LanguageMgmt.LanguageModel response)
        => _client.CreateAsync(
                Arg.Any<LanguageMgmt.ManageLanguageRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGet(LanguageMgmt.LanguageModel response)
        => _client.GetAsync(
                Arg.Any<LanguageMgmt.GetLanguageRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupList(LanguageMgmt.ListLanguagesResponse response)
        => _client.ListAsync(
                Arg.Any<LanguageMgmt.ListLanguagesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupDelete()
        => _client.DeleteAsync(
                Arg.Any<LanguageMgmt.DeleteLanguageRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
