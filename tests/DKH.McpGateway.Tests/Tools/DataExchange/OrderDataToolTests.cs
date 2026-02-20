using DKH.McpGateway.Application.Tools.DataExchange;
using Google.Protobuf;
using OrderDx = DKH.OrderService.Contracts.Order.Api.DataExchange.v1;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public class OrderDataToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly OrderDx.DataExchangeService.DataExchangeServiceClient _client =
        Substitute.For<OrderDx.DataExchangeService.DataExchangeServiceClient>();

    [Fact]
    public async Task Import_HappyPath_WithImportOptionsAsync()
    {
        SetupImport(new OrderDx.ImportResponse { Processed = 5, Failed = 0 });

        await ExecuteToolAsync("import", content: /*lang=json,strict*/ "[{\"orderNumber\":\"O-1\"}]",
            updateExisting: true, skipErrors: true, defaultLanguage: "ru");

        _ = _client.Received(1).ImportAsync(
            Arg.Is<OrderDx.ImportRequest>(r =>
                r.Options.UpdateExisting &&
                r.Options.SkipErrors &&
                r.Options.DefaultLanguage == "ru"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_DefaultImportOptions_UsesCorrectDefaultsAsync()
    {
        SetupImport(new OrderDx.ImportResponse { Processed = 1, Failed = 0 });

        await ExecuteToolAsync("import", content: "[{}]");

        _ = _client.Received(1).ImportAsync(
            Arg.Is<OrderDx.ImportRequest>(r =>
                !r.Options.UpdateExisting &&
                !r.Options.SkipErrors &&
                r.Options.DefaultLanguage == "en"),
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
        SetupExport(new OrderDx.ExportResponse { Content = ByteString.CopyFromUtf8(/*lang=json,strict*/ "{\"orders\":[]}") });

        var result = await ExecuteToolAsync("export");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("content").GetString().Should().Contain("orders");
    }

    [Fact]
    public async Task Export_WithStatusFilter_SetsStatusAsync()
    {
        SetupExport(new OrderDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", status: "Confirmed");

        _ = _client.Received(1).ExportAsync(
            Arg.Is<OrderDx.ExportRequest>(r => r.Status == "Confirmed"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_WithAllFilters_SetsAllFieldsAsync()
    {
        SetupExport(new OrderDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export",
            language: "ru", search: "ORD-", status: "Pending",
            orderBy: "createdAt:desc", page: 3, pageSize: 10);

        _ = _client.Received(1).ExportAsync(
            Arg.Is<OrderDx.ExportRequest>(r =>
                r.Language == "ru" &&
                r.Search == "ORD-" &&
                r.Status == "Pending" &&
                r.OrderBy == "createdAt:desc" &&
                r.Page == 3 &&
                r.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Valid_ReturnsSuccessAsync()
    {
        SetupValidate(new OrderDx.ValidateImportResponse
        {
            Valid = true,
            TotalRecords = 2,
            ValidRecords = 2,
        });

        var result = await ExecuteToolAsync("validate", content: "[{}]");

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
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
        SetupTemplate(new OrderDx.GetImportTemplateResponse
        {
            Filename = "orders_template.json",
            ContentType = "application/json",
            Content = ByteString.CopyFromUtf8("{}"),
        });

        var result = await ExecuteToolAsync("template");

        Parse(result).GetProperty("filename").GetString().Should().Be("orders_template.json");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("purge");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task PermissionDenied_ThrowsUnauthorizedAsync()
    {
        var act = () => OrderDataTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, action: "import", profile: "orders");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ImportAsync(
                Arg.Any<OrderDx.ImportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<OrderDx.ImportResponse>(
                StatusCode.Unavailable));

        var act = () => ExecuteToolAsync("import", content: "[{}]");

        await act.Should().ThrowAsync<RpcException>();
    }

    private Task<string> ExecuteToolAsync(
        string action, string profile = "orders", string format = "json",
        string? content = null, bool? updateExisting = null, bool? skipErrors = null,
        string? defaultLanguage = null, string? language = null, string? search = null,
        string? status = null, string? orderBy = null, int? page = null, int? pageSize = null)
        => OrderDataTool.ExecuteAsync(
            _auth, _client,
            action: action, profile: profile, format: format, content: content,
            updateExisting: updateExisting, skipErrors: skipErrors,
            defaultLanguage: defaultLanguage, language: language,
            search: search, status: status, orderBy: orderBy,
            page: page, pageSize: pageSize);

    private void SetupImport(OrderDx.ImportResponse r) => _client.ImportAsync(
            Arg.Any<OrderDx.ImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupExport(OrderDx.ExportResponse r) => _client.ExportAsync(
            Arg.Any<OrderDx.ExportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupValidate(OrderDx.ValidateImportResponse r) => _client.ValidateImportAsync(
            Arg.Any<OrderDx.ValidateImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupTemplate(OrderDx.GetImportTemplateResponse r) => _client.GetImportTemplateAsync(
            Arg.Any<OrderDx.GetImportTemplateRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
