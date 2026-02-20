using DKH.McpGateway.Application.Tools.DataExchange;
using Google.Protobuf;
using RefDx = DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public class ReferenceDataToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly RefDx.DataExchangeService.DataExchangeServiceClient _client =
        Substitute.For<RefDx.DataExchangeService.DataExchangeServiceClient>();

    [Fact]
    public async Task Import_HappyPath_NoImportOptionsAsync()
    {
        SetupImport(new RefDx.ImportResponse { Processed = 5, Failed = 0 });

        var result = await ExecuteToolAsync("import", "countries", content: /*lang=json,strict*/ "[{\"code\":\"US\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("processed").GetInt32().Should().Be(5);

        _ = _client.Received(1).ImportAsync(
            Arg.Is<RefDx.ImportRequest>(r =>
                r.Profile == "countries" &&
                r.Format == "json" &&
                r.Content.ToStringUtf8() == /*lang=json,strict*/ "[{\"code\":\"US\"}]"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_MissingContent_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("import", "countries");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("content is required");
    }

    [Fact]
    public async Task Import_WithFailures_ReturnsFailedAsync()
    {
        var response = new RefDx.ImportResponse { Processed = 3, Failed = 1 };
        response.Errors.Add("Row 2: invalid ISO code");
        SetupImport(response);

        var result = await ExecuteToolAsync("import", "countries", content: /*lang=json,strict*/ "[{\"code\":\"XX\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("failed").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Export_HappyPath_ReturnsContentAsync()
    {
        SetupExport(new RefDx.ExportResponse { Content = ByteString.CopyFromUtf8(/*lang=json,strict*/ "{\"items\":[]}") });

        var result = await ExecuteToolAsync("export", "currencies");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("profile").GetString().Should().Be("currencies");
    }

    [Fact]
    public async Task Export_WithFilters_SetsRequestFieldsAsync()
    {
        SetupExport(new RefDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", "languages",
            language: "en", search: "english", orderBy: "code:asc", page: 1, pageSize: 50);

        _ = _client.Received(1).ExportAsync(
            Arg.Is<RefDx.ExportRequest>(r =>
                r.Language == "en" &&
                r.Search == "english" &&
                r.OrderBy == "code:asc" &&
                r.Page == 1 &&
                r.PageSize == 50),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Valid_ReturnsSuccessAsync()
    {
        SetupValidate(new RefDx.ValidateImportResponse
        {
            Valid = true,
            TotalRecords = 3,
            ValidRecords = 3,
        });

        var result = await ExecuteToolAsync("validate", "countries", content: /*lang=json,strict*/ "[{\"code\":\"US\"}]");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MissingContent_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("validate", "countries");

        Parse(result).GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Template_HappyPath_ReturnsTemplateAsync()
    {
        SetupTemplate(new RefDx.GetImportTemplateResponse
        {
            Filename = "countries_template.json",
            ContentType = "application/json",
            Content = ByteString.CopyFromUtf8("{}"),
        });

        var result = await ExecuteToolAsync("template", "countries");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("filename").GetString().Should().Be("countries_template.json");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("delete", "countries");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Theory]
    [InlineData("IMPORT")]
    [InlineData("Template")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupImport(new RefDx.ImportResponse { Processed = 1, Failed = 0 });
        SetupTemplate(new RefDx.GetImportTemplateResponse
        {
            Filename = "t.json",
            ContentType = "application/json",
            Content = ByteString.CopyFromUtf8("{}"),
        });

        var result = await ExecuteToolAsync(action, "countries",
            content: action.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "[{}]" : null);

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PermissionDenied_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ReferenceDataTool.ExecuteAsync(
            readOnly, _client, action: "export", profile: "countries");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ExportAsync(
                Arg.Any<RefDx.ExportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<RefDx.ExportResponse>(
                StatusCode.Unavailable));

        var act = () => ExecuteToolAsync("export", "countries");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action, string profile = "countries", string format = "json",
        string? content = null, string? language = null, string? search = null,
        string? orderBy = null, int? page = null, int? pageSize = null)
        => ReferenceDataTool.ExecuteAsync(
            _auth, _client,
            action: action, profile: profile, format: format, content: content,
            language: language, search: search, orderBy: orderBy,
            page: page, pageSize: pageSize);

    private void SetupImport(RefDx.ImportResponse r) => _client.ImportAsync(
            Arg.Any<RefDx.ImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupExport(RefDx.ExportResponse r) => _client.ExportAsync(
            Arg.Any<RefDx.ExportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupValidate(RefDx.ValidateImportResponse r) => _client.ValidateImportAsync(
            Arg.Any<RefDx.ValidateImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupTemplate(RefDx.GetImportTemplateResponse r) => _client.GetImportTemplateAsync(
            Arg.Any<RefDx.GetImportTemplateRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
