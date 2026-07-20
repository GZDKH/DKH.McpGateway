using System.Security.Claims;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Application;
using DKH.McpGateway.Application.Resources;
using DKH.McpGateway.Application.Tools.DataExchange;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace DKH.McpGateway.Tests.Auth;

public class MerchantResourceAuthorizationTests
{
    [Fact]
    public async Task Policy_WithMcpReadAndSingleWorkspace_SucceedsAsync()
    {
        await using var provider = CreateProvider(CreateContext());
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(), null, MerchantResourceAuthorization.PolicyName);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApiKeyScope.Storefront, true, true, true)]
    [InlineData(ApiKeyScope.Mcp, false, true, true)]
    [InlineData(ApiKeyScope.Mcp, true, false, true)]
    [InlineData(ApiKeyScope.Mcp, true, true, false)]
    public async Task Policy_WithInvalidMerchantContext_FailsAsync(
        ApiKeyScope keyScope,
        bool hasReadPermission,
        bool hasWorkspace,
        bool principalAuthenticated)
    {
        var context = CreateContext(keyScope, hasReadPermission, hasWorkspace, principalAuthenticated);
        await using var provider = CreateProvider(context);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(), null, MerchantResourceAuthorization.PolicyName);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_WithDuplicateWorkspaceHeader_FailsAsync()
    {
        var context = CreateContext();
        context.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            new StringValues([Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D")]);
        await using var provider = CreateProvider(context);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(), null, MerchantResourceAuthorization.PolicyName);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_WithoutHttpTransport_FailsAsync()
    {
        await using var provider = CreateProvider(httpContext: null);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(), null, MerchantResourceAuthorization.PolicyName);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void MerchantResourceTypes_DeclareDiscoveryPolicy()
    {
        typeof(CatalogResources).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().ContainSingle(attribute => attribute.Policy == MerchantResourceAuthorization.PolicyName);
        typeof(StorefrontResources).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().ContainSingle(attribute => attribute.Policy == MerchantResourceAuthorization.PolicyName);
    }

    private static ServiceProvider CreateProvider(HttpContext? httpContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IHttpContextAccessor>(new FixedAccessor(httpContext));
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(
        ApiKeyScope keyScope = ApiKeyScope.Mcp,
        bool hasReadPermission = true,
        bool hasWorkspace = true,
        bool principalAuthenticated = true)
    {
        var context = new DefaultHttpContext
        {
            User = principalAuthenticated
                ? new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "merchant-user")], "test"))
                : new ClaimsPrincipal(),
        };
        context.Items["ApiKeyId"] = "mcp-key";
        context.Items["ApiKeyScope"] = keyScope;
        context.Items["ApiKeyPermissions"] = hasReadPermission
            ? new List<string>([McpPermissions.Read])
            : [];
        if (hasWorkspace)
        {
            context.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
                Guid.NewGuid().ToString("D");
        }

        return context;
    }

    private sealed class FixedAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }
}
