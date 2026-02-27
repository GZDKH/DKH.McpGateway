using DKH.ApiManagementService.Contracts.ApiManagement.Api.ApiKeyQuery.v1;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.Platform.Grpc.Common.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DKH.McpGateway.Tests.Auth;

public sealed class ApiKeyAuthMiddlewareTests : IDisposable
{
    private readonly RequestDelegate _next = Substitute.For<RequestDelegate>();
    private readonly ApiKeyQueryService.ApiKeyQueryServiceClient _validationClient =
        Substitute.For<ApiKeyQueryService.ApiKeyQueryServiceClient>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ILogger<ApiKeyAuthMiddleware> _logger = Substitute.For<ILogger<ApiKeyAuthMiddleware>>();
    private readonly ApiKeyAuthMiddleware _middleware;

    public ApiKeyAuthMiddlewareTests()
    {
        _middleware = new ApiKeyAuthMiddleware(_next, _validationClient, _cache, _logger);
    }

    [Fact]
    public async Task InvokeAsync_HealthCheck_SkipsValidationAsync()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/ready";

        await _middleware.InvokeAsync(context);

        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_MissingHeader_Returns401Async()
    {
        var context = new DefaultHttpContext();

        await _middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task InvokeAsync_InvalidKey_Returns401Async()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "invalid-key";

        var response = new ValidateApiKeyResponse { IsValid = false, ErrorReason = "Invalid key" };
        SetupValidation(response);

        await _middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WrongScope_Returns403Async()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "storefront-key";

        var response = new ValidateApiKeyResponse
        {
            IsValid = true,
            Scope = ApiKeyScope.Storefront,
            ApiKeyId = new GuidValue(Guid.NewGuid().ToString())
        };
        SetupValidation(response);

        await _middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_ValidMcpKey_CallsNextAsync()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "valid-mcp-key";

        var response = new ValidateApiKeyResponse
        {
            IsValid = true,
            Scope = ApiKeyScope.Mcp,
            ApiKeyId = new GuidValue(Guid.NewGuid().ToString())
        };
        SetupValidation(response);

        await _middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        await _next.Received(1).Invoke(context);
        context.Items["ApiKeyScope"].Should().Be(ApiKeyScope.Mcp);
    }

    private void SetupValidation(ValidateApiKeyResponse response)
    {
        _validationClient.ValidateApiKeyAsync(Arg.Any<ValidateApiKeyRequest>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}
