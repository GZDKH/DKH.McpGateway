namespace DKH.McpGateway.Tests.Infrastructure;

internal static class ApiKeyContextMocks
{
    internal static IApiKeyContext FullAccess()
    {
        var mock = Substitute.For<IApiKeyContext>();
        mock.IsAuthenticated.Returns(true);
        mock.ApiKeyId.Returns("test-key-id");
        mock.HasPermission(Arg.Any<string>()).Returns(true);
        return mock;
    }

    internal static IApiKeyContext ReadOnly()
    {
        var mock = Substitute.For<IApiKeyContext>();
        mock.IsAuthenticated.Returns(true);
        mock.ApiKeyId.Returns("test-key-id");
        mock.HasPermission(McpPermissions.Read).Returns(true);
        mock.HasPermission(McpPermissions.Write).Returns(false);
        mock.When(x => x.EnsurePermission(McpPermissions.Write))
            .Do(_ => throw new UnauthorizedAccessException(
                "API key does not have required permission: mcp:write"));
        return mock;
    }

    internal static IApiKeyContext Unauthenticated()
    {
        var mock = Substitute.For<IApiKeyContext>();
        mock.IsAuthenticated.Returns(false);
        mock.ApiKeyId.Returns((string?)null);
        mock.HasPermission(Arg.Any<string>()).Returns(false);
        mock.When(x => x.EnsurePermission(Arg.Any<string>()))
            .Do(_ => throw new UnauthorizedAccessException("API key not provided"));
        return mock;
    }
}
