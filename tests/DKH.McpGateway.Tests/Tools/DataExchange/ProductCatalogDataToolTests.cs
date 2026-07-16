using System.Security.Claims;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Application.Tools.DataExchange;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using CatalogDx = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public class ProductCatalogDataToolTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly HttpContextAccessor _httpContextAccessor = new();

    private readonly CatalogDx.DataExchangeService.DataExchangeServiceClient _client =
        Substitute.For<CatalogDx.DataExchangeService.DataExchangeServiceClient>();

    public ProductCatalogDataToolTests()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString("D"))],
            authenticationType: "Test"));
        _httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            _workspaceId.ToString("D");
        _httpContextAccessor.HttpContext = _httpContext;
    }

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
            Arg.Is<Metadata>(metadata => HasExpectedWorkspaceMetadata(metadata)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
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
            Arg.Is<Metadata>(metadata => HasExpectedWorkspaceMetadata(metadata)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
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

        _ = _client.Received(1).ValidateImportAsync(
            Arg.Any<CatalogDx.ValidateImportRequest>(),
            Arg.Is<Metadata>(metadata => HasExpectedWorkspaceMetadata(metadata)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
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
            Arg.Is<Metadata>(metadata => HasExpectedWorkspaceMetadata(metadata)),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("unknown");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("Unknown action");
        _auth.DidNotReceive().EnsurePermission(Arg.Any<string>());
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownAction_WithStorefrontScope_FailsAtAuthenticationBoundaryAsync()
    {
        var storefront = ApiKeyContextMocks.FullAccess();
        storefront.Scope.Returns(ApiKeyScope.Storefront);

        var act = () => ExecuteToolAsync("unknown", auth: storefront);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*MCP-scoped*");
        storefront.DidNotReceive().EnsurePermission(Arg.Any<string>());
        _client.ReceivedCalls().Should().BeEmpty();
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

    [Theory]
    [InlineData("export")]
    [InlineData("validate")]
    [InlineData("template")]
    public async Task ReadActions_WithReadOnlyKey_RequireReadPermissionAsync(string action)
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();
        SetupReadAction(action);

        var result = await ExecuteToolAsync(
            action,
            content: action == "validate" ? "[{}]" : null,
            auth: readOnly);

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
        readOnly.Received(1).EnsurePermission(McpPermissions.Read);
    }

    [Fact]
    public async Task PermissionDenied_ThrowsUnauthorizedAsync()
    {
        var readOnly = ApiKeyContextMocks.ReadOnly();

        var act = () => ProductCatalogDataTool.ExecuteAsync(
            readOnly, _httpContextAccessor, _client, action: "import", profile: "products");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        readOnly.Received(1).EnsurePermission(McpPermissions.Write);
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task InvalidWorkspaceHeader_FailsBeforeGrpcAsync(string value)
    {
        _httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] = value;

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.InvalidArgument);
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task MissingWorkspaceHeader_FailsBeforeGrpcAsync()
    {
        _httpContext.Request.Headers.Remove(ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName);

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.InvalidArgument);
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task DuplicateWorkspaceHeader_FailsBeforeGrpcAsync()
    {
        _httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            new StringValues([Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D")]);

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.InvalidArgument);
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(ApiKeyScope.Storefront)]
    [InlineData(ApiKeyScope.Unspecified)]
    public async Task NonAdminScope_FailsBeforeGrpcAsync(ApiKeyScope scope)
    {
        var nonAdmin = ApiKeyContextMocks.FullAccess();
        nonAdmin.Scope.Returns(scope);

        var act = () => ExecuteToolAsync("export", auth: nonAdmin);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*MCP-scoped*");
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task NoHttpContext_RejectsStdioStyleExecutionBeforeGrpcAsync()
    {
        _httpContextAccessor.HttpContext = null;

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*HTTP transport*");
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedHttpPrincipal_FailsBeforeGrpcAsync()
    {
        _httpContext.User = new ClaimsPrincipal();

        var act = () => ExecuteToolAsync("export");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*HTTP principal*");
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedApiKey_FailsBeforeGrpcAsync()
    {
        var act = () => ExecuteToolAsync("export", auth: ApiKeyContextMocks.Unauthenticated());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _client.ReceivedCalls().Should().BeEmpty();
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
        int? pageSize = null,
        IApiKeyContext? auth = null)
        => ProductCatalogDataTool.ExecuteAsync(
            auth ?? _auth, _httpContextAccessor, _client,
            action: action, profile: profile, format: format, content: content,
            createMissingGroups: createMissingGroups,
            createMissingAttributes: createMissingAttributes,
            createMissingOptions: createMissingOptions,
            language: language, search: search, published: published,
            orderBy: orderBy, page: page, pageSize: pageSize);

    private bool HasExpectedWorkspaceMetadata(Metadata metadata)
        => metadata.Count == 1
           && metadata[0].Key == ProductCatalogWorkspaceRequestContext.GrpcWorkspaceIdHeaderName
           && metadata[0].Value == _workspaceId.ToString("D");

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

    private void SetupReadAction(string action)
    {
        if (action == "export")
        {
            SetupExport(new CatalogDx.ExportResponse { Content = ByteString.CopyFromUtf8("{}") });
            return;
        }

        if (action == "validate")
        {
            SetupValidate(new CatalogDx.ValidateImportResponse { Valid = true });
            return;
        }

        SetupTemplate(new CatalogDx.GetImportTemplateResponse
        {
            Content = ByteString.CopyFromUtf8("{}"),
        });
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
