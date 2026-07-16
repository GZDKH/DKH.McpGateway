using System.Security.Claims;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.McpGateway.Application.Tools.DataExchange;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace DKH.McpGateway.Tests.Tools.DataExchange;

public sealed class ProductCatalogWorkspaceRequestContextTests
{
    [Fact]
    public void CreateRequiredGrpcMetadata_WithAuthenticatedMcpRequest_ReturnsNormalizedWorkspaceOnly()
    {
        var workspaceId = Guid.NewGuid();
        var httpContext = CreateHttpContext(workspaceId);
        httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            workspaceId.ToString("B").ToUpperInvariant();
        httpContext.Request.Headers["X-Untrusted-Metadata"] = "must-not-forward";

        var metadata = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp),
            new HttpContextAccessor { HttpContext = httpContext });

        metadata.Should().ContainSingle();
        metadata[0].Key.Should().Be(ProductCatalogWorkspaceRequestContext.GrpcWorkspaceIdHeaderName);
        metadata[0].Value.Should().Be(workspaceId.ToString("D"));
    }

    [Fact]
    public void CreateRequiredGrpcMetadata_WithoutWorkspaceHeader_ThrowsInvalidArgument()
    {
        var httpContext = CreateHttpContext(Guid.NewGuid());
        httpContext.Request.Headers.Remove(ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName);

        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp),
            new HttpContextAccessor { HttpContext = httpContext });

        act.Should().Throw<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void CreateRequiredGrpcMetadata_WithInvalidWorkspaceHeader_ThrowsInvalidArgument(string value)
    {
        var httpContext = CreateHttpContext(Guid.NewGuid());
        httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] = value;

        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp),
            new HttpContextAccessor { HttpContext = httpContext });

        act.Should().Throw<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.InvalidArgument);
    }

    [Fact]
    public void CreateRequiredGrpcMetadata_WithDuplicateWorkspaceHeader_ThrowsInvalidArgument()
    {
        var httpContext = CreateHttpContext(Guid.NewGuid());
        httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            new StringValues([Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D")]);

        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp),
            new HttpContextAccessor { HttpContext = httpContext });

        act.Should().Throw<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(ApiKeyScope.Storefront)]
    [InlineData(ApiKeyScope.Unspecified)]
    public void CreateRequiredGrpcMetadata_WithNonAdminScope_ThrowsBeforeReadingWorkspace(ApiKeyScope scope)
    {
        var httpContext = CreateHttpContext(Guid.NewGuid());

        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(scope),
            new HttpContextAccessor { HttpContext = httpContext });

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*MCP-scoped*");
    }

    [Fact]
    public void CreateRequiredGrpcMetadata_WithUnauthenticatedApiKey_Throws()
    {
        var httpContext = CreateHttpContext(Guid.NewGuid());

        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp, isAuthenticated: false),
            new HttpContextAccessor { HttpContext = httpContext });

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*MCP-scoped*");
    }

    [Fact]
    public void CreateRequiredGrpcMetadata_WithoutHttpContext_RejectsStdioStyleExecution()
    {
        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp),
            new HttpContextAccessor());

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*HTTP transport*");
    }

    [Fact]
    public void CreateRequiredGrpcMetadata_WithoutAuthenticatedPrincipal_Throws()
    {
        var httpContext = CreateHttpContext(Guid.NewGuid(), authenticatedPrincipal: false);

        var act = () => ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            CreateApiKeyContext(ApiKeyScope.Mcp),
            new HttpContextAccessor { HttpContext = httpContext });

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*HTTP principal*");
    }

    private static DefaultHttpContext CreateHttpContext(
        Guid workspaceId,
        bool authenticatedPrincipal = true)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticatedPrincipal)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", Guid.NewGuid().ToString("D"))],
                authenticationType: "Test"));
        }

        httpContext.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            workspaceId.ToString("D");
        return httpContext;
    }

    private static IApiKeyContext CreateApiKeyContext(
        ApiKeyScope scope,
        bool isAuthenticated = true)
    {
        var context = Substitute.For<IApiKeyContext>();
        context.IsAuthenticated.Returns(isAuthenticated);
        context.Scope.Returns(scope);
        return context;
    }
}
