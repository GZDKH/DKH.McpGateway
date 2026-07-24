using System.Security.Claims;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using Microsoft.AspNetCore.Authorization;

namespace DKH.McpGateway.Tests.Auth;

public sealed class McpIngressAuthorizationHandlerTests
{
    private static async Task<bool> EvaluateAsync(bool authenticated, ApiKeyScope scope)
    {
        var apiKey = Substitute.For<IApiKeyContext>();
        apiKey.IsAuthenticated.Returns(authenticated);
        apiKey.Scope.Returns(scope);

        var requirement = new McpIngressRequirement();
        var context = new AuthorizationHandlerContext(
            [requirement], new ClaimsPrincipal(new ClaimsIdentity()), resource: null);

        await new McpIngressAuthorizationHandler(apiKey).HandleAsync(context);
        return context.HasSucceeded;
    }

    [Theory]
    [InlineData(ApiKeyScope.Storefront)]
    [InlineData(ApiKeyScope.Mcp)]
    public async Task Succeeds_ForValidMcpEligibleKeyAsync(ApiKeyScope scope)
        => (await EvaluateAsync(authenticated: true, scope)).Should().BeTrue();

    [Fact]
    public async Task Fails_WhenNotAuthenticatedAsync()
        => (await EvaluateAsync(authenticated: false, ApiKeyScope.Storefront)).Should().BeFalse();

    [Fact]
    public async Task Fails_ForUnspecifiedScopeAsync()
        => (await EvaluateAsync(authenticated: true, ApiKeyScope.Unspecified)).Should().BeFalse();
}
