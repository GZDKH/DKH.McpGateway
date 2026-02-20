using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Tests.Auth;

public class HttpApiKeyContextTests
{
    private readonly DefaultHttpContext _httpContext = new();
    private readonly HttpApiKeyContext _sut;

    public HttpApiKeyContextTests()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(_httpContext);
        _sut = new HttpApiKeyContext(accessor);
    }

    [Fact]
    public void IsAuthenticated_WithApiKeyId_ReturnsTrue()
    {
        _httpContext.Items["ApiKeyId"] = "key-123";

        _sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithoutApiKeyId_ReturnsFalse()
    {
        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WithMatchingPermission_ReturnsTrue()
    {
        _httpContext.Items["ApiKeyPermissions"] =
            new List<string> { "mcp:read", "mcp:write" };

        _sut.HasPermission("mcp:read").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WithMissingPermission_ReturnsFalse()
    {
        _httpContext.Items["ApiKeyPermissions"] =
            new List<string> { "mcp:read" };

        _sut.HasPermission("mcp:write").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_CaseInsensitive()
    {
        _httpContext.Items["ApiKeyPermissions"] =
            new List<string> { "MCP:READ" };

        _sut.HasPermission("mcp:read").Should().BeTrue();
    }

    [Fact]
    public void EnsurePermission_WithPermission_DoesNotThrow()
    {
        _httpContext.Items["ApiKeyId"] = "key-123";
        _httpContext.Items["ApiKeyPermissions"] =
            new List<string> { "mcp:write" };

        var act = () => _sut.EnsurePermission("mcp:write");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePermission_WithoutPermission_Throws()
    {
        _httpContext.Items["ApiKeyId"] = "key-123";
        _httpContext.Items["ApiKeyPermissions"] =
            new List<string> { "mcp:read" };

        var act = () => _sut.EnsurePermission("mcp:write");

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*mcp:write*");
    }

    [Fact]
    public void EnsurePermission_Unauthenticated_Throws()
    {
        var act = () => _sut.EnsurePermission("mcp:read");

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*not provided*");
    }

    [Fact]
    public void Scope_WithScopeSet_ReturnsValue()
    {
        _httpContext.Items["ApiKeyScope"] = ApiKeyScope.Storefront;

        _sut.Scope.Should().Be(ApiKeyScope.Storefront);
    }

    [Fact]
    public void Scope_WithoutScopeSet_ReturnsUnspecified()
    {
        _sut.Scope.Should().Be(ApiKeyScope.Unspecified);
    }

    [Fact]
    public void Permissions_WithNoItems_ReturnsEmpty()
    {
        _sut.Permissions.Should().BeEmpty();
    }
}
