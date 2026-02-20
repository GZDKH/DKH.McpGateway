using DKH.McpGateway.Application.Tools.DataExchange;
using Google.Protobuf;
using CatalogDx = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public class ProductCatalogDataToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly CatalogDx.DataExchangeService.DataExchangeServiceClient _client =
        Substitute.For<CatalogDx.DataExchangeService.DataExchangeServiceClient>();

    [Fact]
    public async Task Import_HappyPath_ReturnsSuccessAsync()
    {
        SetupImport(new CatalogDx.ImportResponse { Processed = 10, Failed = 0 });

        var result = await ExecuteToolAsync("import", content: /*lang=json,strict*/ "[{\"code\":\"P1\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("processed").GetInt32().Should().Be(10);
        json.GetProperty("failed").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Import_MissingContent_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("import", content: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("content is required");
    }

    [Fact]
    public async Task Import_WithFailures_ReturnsFailedCountAsync()
    {
        var response = new CatalogDx.ImportResponse { Processed = 10, Failed = 3 };
        response.Errors.Add("Row 2: invalid code");
        response.Errors.Add("Row 5: duplicate");
        response.Errors.Add("Row 8: missing name");
        SetupImport(response);

        var result = await ExecuteToolAsync("import", content: /*lang=json,strict*/ "[{\"code\":\"P1\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("failed").GetInt32().Should().Be(3);
        json.GetProperty("errors").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Import_SetsCreateMissingFlagsAsync()
    {
        SetupImport(new CatalogDx.ImportResponse { Processed = 1, Failed = 0 });

        await ExecuteToolAsync("import",
            content: /*lang=json,strict*/ "[{\"code\":\"P1\"}]",
            createMissingGroups: false,
            createMissingAttributes: false,
            createMissingOptions: false);

        _ = _client.Received(1).ImportAsync(
            Arg.Is<CatalogDx.ImportRequest>(r =>
                !r.Options.CreateMissingGroups &&
                !r.Options.CreateMissingAttributes &&
                !r.Options.CreateMissingOptions),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_DefaultOptions_UsesCorrectDefaultsAsync()
    {
        SetupImport(new CatalogDx.ImportResponse { Processed = 1, Failed = 0 });

        await ExecuteToolAsync("import", content: /*lang=json,strict*/ "[{\"code\":\"P1\"}]");

        _ = _client.Received(1).ImportAsync(
            Arg.Is<CatalogDx.ImportRequest>(r =>
                !r.Options.UpdateExisting &&
                !r.Options.SkipErrors &&
                r.Options.CreateMissingGroups &&
                r.Options.CreateMissingAttributes &&
                r.Options.CreateMissingOptions &&
                r.Options.DefaultLanguage == "en"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_HappyPath_ReturnsContentAsync()
    {
        var exportContent = /*lang=json,strict*/ "{\"items\":[{\"code\":\"P1\"}]}";
        SetupExport(new CatalogDx.ExportResponse { Content = ByteString.CopyFromUtf8(exportContent) });

        var result = await ExecuteToolAsync("export");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("content").GetString().Should().Be(exportContent);
        json.GetProperty("format").GetString().Should().Be("json");
        json.GetProperty("profile").GetString().Should().Be("products");
    }

    [Fact]
    public async Task Export_WithAllFilters_SetsAllRequestFieldsAsync()
    {
        SetupExport(new CatalogDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export",
            language: "ru", search: "test", published: "true",
            orderBy: "name:asc", page: 2, pageSize: 25);

        _ = _client.Received(1).ExportAsync(
            Arg.Is<CatalogDx.ExportRequest>(r =>
                r.Language == "ru" &&
                r.Search == "test" &&
                r.Published == "true" &&
                r.OrderBy == "name:asc" &&
                r.Page == 2 &&
                r.PageSize == 25),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Valid_ReturnsSuccessAsync()
    {
        SetupValidate(new CatalogDx.ValidateImportResponse
        {
            Valid = true,
            TotalRecords = 5,
            ValidRecords = 5,
        });

        var result = await ExecuteToolAsync("validate", content: /*lang=json,strict*/ "[{\"code\":\"P1\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("totalRecords").GetInt32().Should().Be(5);
        json.GetProperty("validRecords").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task Validate_WithErrorsAndWarnings_ReturnsDetailsAsync()
    {
        var response = new CatalogDx.ValidateImportResponse
        {
            Valid = false,
            TotalRecords = 3,
            ValidRecords = 1,
        };
        response.Errors.Add(new CatalogDx.ValidationError
        {
            Line = 2,
            Field = "code",
            Message = "duplicate",
            Value = "P1",
        });
        response.Warnings.Add(new CatalogDx.ValidationWarning
        {
            Line = 3,
            Field = "description",
            Message = "too short",
        });
        SetupValidate(response);

        var result = await ExecuteToolAsync("validate", content: /*lang=json,strict*/ "[{\"code\":\"P1\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("errors").GetArrayLength().Should().Be(1);
        json.GetProperty("warnings").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Validate_MissingContent_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("validate", content: null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("content is required");
    }

    [Fact]
    public async Task Template_HappyPath_ReturnsTemplateAsync()
    {
        SetupTemplate(new CatalogDx.GetImportTemplateResponse
        {
            Filename = "products_template.json",
            ContentType = "application/json",
            Content = ByteString.CopyFromUtf8(/*lang=json,strict*/ "{\"columns\":[]}"),
        });

        var result = await ExecuteToolAsync("template");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("filename").GetString().Should().Be("products_template.json");
        json.GetProperty("contentType").GetString().Should().Be("application/json");
    }

    [Fact]
    public async Task Template_DefaultIncludeExample_IsTrueAsync()
    {
        SetupTemplate(new CatalogDx.GetImportTemplateResponse
        {
            Filename = "t.json",
            ContentType = "application/json",
            Content = ByteString.CopyFromUtf8("{}"),
        });

        await ExecuteToolAsync("template");

        _ = _client.Received(1).GetImportTemplateAsync(
            Arg.Is<CatalogDx.GetImportTemplateRequest>(r => r.IncludeExample),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
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
    [InlineData("IMPORT")]
    [InlineData("Import")]
    [InlineData("EXPORT")]
    [InlineData("Export")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupImport(new CatalogDx.ImportResponse { Processed = 1, Failed = 0 });
        SetupExport(new CatalogDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        var result = await ExecuteToolAsync(action,
            content: action.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "[{}]" : null);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PermissionDenied_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ProductCatalogDataTool.ExecuteAsync(
            readOnly, _client, action: "import", profile: "products");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ExportAsync(
                Arg.Any<CatalogDx.ExportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CatalogDx.ExportResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task GrpcDeadlineExceeded_ThrowsRpcExceptionAsync()
    {
        _client.ImportAsync(
                Arg.Any<CatalogDx.ImportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CatalogDx.ImportResponse>(
                StatusCode.DeadlineExceeded, "Deadline exceeded"));

        var act = () => ExecuteToolAsync("import", content: "[{}]");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.DeadlineExceeded);
    }

    private Task<string> ExecuteToolAsync(
        string action,
        string profile = "products",
        string format = "json",
        string? content = null,
        bool? createMissingGroups = null,
        bool? createMissingAttributes = null,
        bool? createMissingOptions = null,
        string? language = null,
        string? search = null,
        string? published = null,
        string? orderBy = null,
        int? page = null,
        int? pageSize = null)
        => ProductCatalogDataTool.ExecuteAsync(
            _auth, _client,
            action: action, profile: profile, format: format, content: content,
            createMissingGroups: createMissingGroups,
            createMissingAttributes: createMissingAttributes,
            createMissingOptions: createMissingOptions,
            language: language, search: search, published: published,
            orderBy: orderBy, page: page, pageSize: pageSize);

    private void SetupImport(CatalogDx.ImportResponse response)
        => _client.ImportAsync(
                Arg.Any<CatalogDx.ImportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupExport(CatalogDx.ExportResponse response)
        => _client.ExportAsync(
                Arg.Any<CatalogDx.ExportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupValidate(CatalogDx.ValidateImportResponse response)
        => _client.ValidateImportAsync(
                Arg.Any<CatalogDx.ValidateImportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupTemplate(CatalogDx.GetImportTemplateResponse response)
        => _client.GetImportTemplateAsync(
                Arg.Any<CatalogDx.GetImportTemplateRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
