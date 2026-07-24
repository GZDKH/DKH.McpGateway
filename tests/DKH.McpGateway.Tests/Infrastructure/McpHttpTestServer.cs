using System.Security.Claims;
using System.Text.Encodings.Web;
using DKH.ApiManagementService.Contracts.ApiManagement.Api.ApiKeyQuery.v1;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Api;
using DKH.McpGateway.Application;
using DKH.Platform.Grpc.Common.Types;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DKH.McpGateway.Tests.Infrastructure;

/// <summary>
/// In-process host that boots the real MCP HTTP pipeline — API-key middleware, ingress policy,
/// SDK MCP server with <c>AddAuthorizationFilters()</c> + <c>WithSurfaceAuthorization()</c>, and
/// <c>MapMcpGateway()</c> — over <see cref="TestServer"/>, with downstream gRPC clients and the
/// bearer principal stubbed. It drives the genuine MCP protocol (initialize/list/call) so the
/// credential matrix for issue #86 is exercised end to end, not just at the unit level.
/// </summary>
internal sealed class McpHttpTestServer : IAsyncDisposable
{
    internal const string TestBearerScheme = "TestBearer";
    internal const string AdminRole = "mcp-admin";

    private readonly WebApplication _app;
    private readonly Dictionary<string, ValidateApiKeyResponse> _keys = new(StringComparer.Ordinal);
    private bool _mcpEnabled = true;

    private McpHttpTestServer()
    {
        // CreateSlimBuilder defaults to the Production environment, so DI validation is off and the
        // MCP tools' many gRPC client dependencies (resolved lazily per invocation) do not need to
        // be registered up front — only the ones the exercised paths touch.
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();

        // Hand-written thread-safe fake gRPC clients (NSubstitute's shared context is not
        // concurrency-safe). Only the API-key validation and storefront tool clients are needed —
        // the metrics usage recorder is deliberately not mapped below.
        builder.Services.AddSingleton<ApiKeyQueryService.ApiKeyQueryServiceClient>(new FakeApiKeyQueryClient(Resolve));
        builder.Services.AddSingleton<StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient>(
            new FakeStorefrontFeaturesClient(() => _mcpEnabled));
        builder.Services.AddSingleton<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>(
            new FakeStorefrontCatalogsClient());

        builder.Services.AddApplication();
        builder.Services
            .AddMcpGatewayServer()
            .AddAuthorizationFilters()
#pragma warning disable MCP9004 // Match the production HTTP transport (legacy SSE retained).
            .WithHttpTransport(static options => options.EnableLegacySse = true)
#pragma warning restore MCP9004
            .WithSurfaceAuthorization();

        builder.Services
            .AddAuthentication(TestBearerScheme)
            .AddScheme<AuthenticationSchemeOptions, TestBearerAuthHandler>(TestBearerScheme, null);
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(McpAuthorizationPolicies.McpAccess, policy => policy.RequireRole(AdminRole));

        _app = builder.Build();
        _app.UseAuthentication();
        // Only the API-key authentication middleware — not UseApiKeyAuth(), which also adds the
        // fire-and-forget usage recorder that touches HttpContext after the response completes and
        // races TestHost's feature-collection teardown. Usage metrics are outside this test's scope.
        _app.UseMiddleware<ApiKeyAuthMiddleware>();
        _app.UseAuthorization();
        _app.MapMcpGateway();
    }

    internal static async Task<McpHttpTestServer> StartAsync(Action<McpHttpTestServer>? configure = null)
    {
        var server = new McpHttpTestServer();
        configure?.Invoke(server);
        await server._app.StartAsync();
        return server;
    }

    /// <summary>Registers a key the validation service will accept with the given scope/binding.</summary>
    internal McpHttpTestServer WithKey(
        string rawKey, ApiKeyScope scope, Guid? storefrontId = null, params string[] permissions)
    {
        var response = new ValidateApiKeyResponse
        {
            IsValid = true,
            Scope = scope,
            ApiKeyId = new GuidValue(Guid.NewGuid().ToString()),
        };
        response.Permissions.AddRange(permissions);
        if (storefrontId is not null)
        {
            response.StorefrontId = new GuidValue(storefrontId.Value.ToString());
        }

        _keys[rawKey] = response;
        return this;
    }

    /// <summary>Toggles the storefront McpEnabled feature gate for the storefront tools.</summary>
    internal McpHttpTestServer WithMcpEnabled(bool enabled)
    {
        _mcpEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Opens a raw JSON-RPC MCP session over real HTTP with the given API key and optional admin
    /// bearer, and completes the initialize handshake. Throws when ingress rejects the credential
    /// (mirrors a real client's failed initialize). Raw request/response POSTs are used instead of
    /// the SDK client so each exchange is independent — no background SSE stream races with the
    /// per-request headers under the in-memory TestServer.
    /// </summary>
    internal async Task<RawMcpSession> ConnectAsync(string? apiKey, bool withAdminBearer = false)
    {
        var httpClient = _app.GetTestServer().CreateClient();
        var bearer = withAdminBearer ? $"{TestBearerScheme} {AdminRole}" : null;
        var session = new RawMcpSession(httpClient, apiKey, bearer);
        await session.InitializeAsync();
        return session;
    }

    private ValidateApiKeyResponse Resolve(ValidateApiKeyRequest request)
        => _keys.TryGetValue(request.RawKey, out var response)
            ? response
            : new ValidateApiKeyResponse { IsValid = false, ErrorReason = "Invalid key" };

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Minimal bearer scheme: an <c>Authorization: TestBearer &lt;role&gt;</c> header authenticates a
    /// principal carrying that role, so the <see cref="McpAuthorizationPolicies.McpAccess"/> role
    /// policy can be satisfied deterministically without Keycloak.
    /// </summary>
    private sealed class TestBearerAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(header) || !header.StartsWith($"{TestBearerScheme} ", StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var roles = header[(TestBearerScheme.Length + 1)..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var claims = new List<Claim> { new(ClaimTypes.Name, "test-admin") };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, TestBearerScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), TestBearerScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
