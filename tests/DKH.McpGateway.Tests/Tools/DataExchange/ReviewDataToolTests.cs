using DKH.McpGateway.Application.Tools.DataExchange;
using Google.Protobuf;
using ReviewDx = DKH.ReviewService.Contracts.Review.Api.DataExchange.v1;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public class ReviewDataToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly ReviewDx.DataExchangeService.DataExchangeServiceClient _client =
        Substitute.For<ReviewDx.DataExchangeService.DataExchangeServiceClient>();

    [Fact]
    public async Task Import_HappyPath_WithImportOptionsAsync()
    {
        SetupImport(new ReviewDx.ImportResponse { Processed = 3, Failed = 0 });

        var result = await ExecuteToolAsync("import", content: /*lang=json,strict*/ "[{\"title\":\"Great\"}]",
            updateExisting: true);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("processed").GetInt32().Should().Be(3);

        _ = _client.Received(1).ImportAsync(
            Arg.Is<ReviewDx.ImportRequest>(r => r.Options.UpdateExisting),
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
        SetupExport(new ReviewDx.ExportResponse { Content = ByteString.CopyFromUtf8(/*lang=json,strict*/ "{\"reviews\":[]}") });

        var result = await ExecuteToolAsync("export");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("profile").GetString().Should().Be("reviews");
    }

    [Fact]
    public async Task Export_WithPagination_UsesPaginationRequestAsync()
    {
        SetupExport(new ReviewDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", page: 2, pageSize: 25);

        _ = _client.Received(1).ExportAsync(
            Arg.Is<ReviewDx.ExportRequest>(r =>
                r.Pagination != null &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 25),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_NoPagination_DoesNotSetPaginationRequestAsync()
    {
        SetupExport(new ReviewDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export");

        _ = _client.Received(1).ExportAsync(
            Arg.Is<ReviewDx.ExportRequest>(r => r.Pagination == null),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_WithStatusFilter_SetsStatusAsync()
    {
        SetupExport(new ReviewDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", status: "Approved");

        _ = _client.Received(1).ExportAsync(
            Arg.Is<ReviewDx.ExportRequest>(r => r.Status == "Approved"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_WithAllFilters_SetsAllFieldsAsync()
    {
        SetupExport(new ReviewDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export",
            language: "ru", search: "great", status: "Pending",
            orderBy: "rating:desc", page: 1, pageSize: 50);

        _ = _client.Received(1).ExportAsync(
            Arg.Is<ReviewDx.ExportRequest>(r =>
                r.Language == "ru" &&
                r.Search == "great" &&
                r.Status == "Pending" &&
                r.OrderBy == "rating:desc" &&
                r.Pagination.Page == 1 &&
                r.Pagination.PageSize == 50),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_OnlyPageSize_SetsPaginationWithDefaultsAsync()
    {
        SetupExport(new ReviewDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });

        await ExecuteToolAsync("export", pageSize: 200);

        _ = _client.Received(1).ExportAsync(
            Arg.Is<ReviewDx.ExportRequest>(r =>
                r.Pagination.Page == 1 &&
                r.Pagination.PageSize == 200),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Valid_ReturnsSuccessAsync()
    {
        SetupValidate(new ReviewDx.ValidateImportResponse
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
        SetupTemplate(new ReviewDx.GetImportTemplateResponse
        {
            Filename = "reviews_template.json",
            ContentType = "application/json",
            Content = ByteString.CopyFromUtf8("{}"),
        });

        var result = await ExecuteToolAsync("template");

        Parse(result).GetProperty("filename").GetString().Should().Be("reviews_template.json");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("archive");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task PermissionDenied_ThrowsUnauthorizedAsync()
    {
        var act = () => ReviewDataTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, action: "import", profile: "reviews");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcDeadlineExceeded_ThrowsRpcExceptionAsync()
    {
        _client.ExportAsync(
                Arg.Any<ReviewDx.ExportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ReviewDx.ExportResponse>(
                StatusCode.DeadlineExceeded));

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.DeadlineExceeded);
    }

    private Task<string> ExecuteToolAsync(
        string action, string profile = "reviews", string format = "json",
        string? content = null, bool? updateExisting = null, bool? skipErrors = null,
        string? defaultLanguage = null, string? language = null, string? search = null,
        string? status = null, string? orderBy = null, int? page = null, int? pageSize = null)
        => ReviewDataTool.ExecuteAsync(
            _auth, _client,
            action: action, profile: profile, format: format, content: content,
            updateExisting: updateExisting, skipErrors: skipErrors,
            defaultLanguage: defaultLanguage, language: language,
            search: search, status: status, orderBy: orderBy,
            page: page, pageSize: pageSize);

    private void SetupImport(ReviewDx.ImportResponse r) => _client.ImportAsync(
            Arg.Any<ReviewDx.ImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupExport(ReviewDx.ExportResponse r) => _client.ExportAsync(
            Arg.Any<ReviewDx.ExportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupValidate(ReviewDx.ValidateImportResponse r) => _client.ValidateImportAsync(
            Arg.Any<ReviewDx.ValidateImportRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private void SetupTemplate(ReviewDx.GetImportTemplateResponse r) => _client.GetImportTemplateAsync(
            Arg.Any<ReviewDx.GetImportTemplateRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(r));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
