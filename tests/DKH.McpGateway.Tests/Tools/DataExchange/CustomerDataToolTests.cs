using DKH.McpGateway.Application.Tools.DataExchange;
using Google.Protobuf;
using CustDx = DKH.CustomerService.Contracts.Customer.Api.DataExchange.v1;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public class CustomerDataToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly CustDx.DataExchangeService.DataExchangeServiceClient _client =
        Substitute.For<CustDx.DataExchangeService.DataExchangeServiceClient>();

    [Fact]
    public async Task Import_HappyPath_WithImportOptionsAsync()
    {
        SetupImport(new CustDx.ImportResponse { Processed = 8, Failed = 0 });

        await ExecuteToolAsync("import", content: /*lang=json,strict*/ "[{\"email\":\"a@b.com\"}]",
            updateExisting: true, skipErrors: false, defaultLanguage: "ru");

        _ = _client.Received(1).ImportAsync(
            Arg.Is<CustDx.ImportRequest>(r =>
                r.Options.UpdateExisting &&
                !r.Options.SkipErrors &&
                r.Options.DefaultLanguage == "ru"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_MissingContent_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("import");

        Parse(result).GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Export_HappyPath_ReturnsContentAsync()
    {
        SetupExport(new CustDx.ExportResponse { Content = ByteString.CopyFromUtf8(/*lang=json,strict*/ "{\"items\":[]}") });

        var result = await ExecuteToolAsync("export");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("profile").GetString().Should().Be("customers");
    }

    [Fact]
    public async Task Export_WithStatusFilter_SetsStatusAsync()
    {
        SetupExport(new CustDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", status: "Active");

        _ = _client.Received(1).ExportAsync(
            Arg.Is<CustDx.ExportRequest>(r => r.Status == "Active"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_WithSearchFilter_SetsSearchAsync()
    {
        SetupExport(new CustDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", search: "john");

        _ = _client.Received(1).ExportAsync(
            Arg.Is<CustDx.ExportRequest>(r => r.Search == "john"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Valid_ReturnsSuccessAsync()
    {
        SetupValidate(new CustDx.ValidateImportResponse
        {
            Valid = true,
            TotalRecords = 4,
            ValidRecords = 4,
        });

        var result = await ExecuteToolAsync("validate", content: "[{}]");

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
        Parse(result).GetProperty("totalRecords").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Validate_MissingContent_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("validate");

        Parse(result).GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Template_HappyPath_ReturnsTemplateAsync()
    {
        SetupTemplate(new CustDx.GetImportTemplateResponse
        {
            Filename = "customers_template.csv",
            ContentType = "text/csv",
            Content = ByteString.CopyFromUtf8("email,name"),
        });

        var result = await ExecuteToolAsync("template", format: "csv");

        var json = Parse(result);
        json.GetProperty("filename").GetString().Should().Be("customers_template.csv");
        json.GetProperty("contentType").GetString().Should().Be("text/csv");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("merge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task PermissionDenied_ThrowsUnauthorizedAsync()
    {
        var act = () => CustomerDataTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, action: "import", profile: "customers");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => CustomerDataTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, action: "export", profile: "customers");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private Task<string> ExecuteToolAsync(
        string action, string profile = "customers", string format = "json",
        string? content = null, bool? updateExisting = null, bool? skipErrors = null,
        string? defaultLanguage = null, string? language = null, string? search = null,
        string? status = null, string? orderBy = null, int? page = null, int? pageSize = null)
        => CustomerDataTool.ExecuteAsync(
            _auth, _client,
            action: action, profile: profile, format: format, content: content,
            updateExisting: updateExisting, skipErrors: skipErrors,
            defaultLanguage: defaultLanguage, language: language,
            search: search, status: status, orderBy: orderBy,
            page: page, pageSize: pageSize);

    private void SetupImport(CustDx.ImportResponse r) => _client.ImportAsync(
            Arg.Any<CustDx.ImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupExport(CustDx.ExportResponse r) => _client.ExportAsync(
            Arg.Any<CustDx.ExportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupValidate(CustDx.ValidateImportResponse r) => _client.ValidateImportAsync(
            Arg.Any<CustDx.ValidateImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupTemplate(CustDx.GetImportTemplateResponse r) => _client.GetImportTemplateAsync(
            Arg.Any<CustDx.GetImportTemplateRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
