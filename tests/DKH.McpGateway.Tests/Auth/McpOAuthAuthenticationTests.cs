using System.Text.Json.Nodes;
using DKH.McpGateway.Api;
using DKH.Platform.Authentication.Keycloak;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;

namespace DKH.McpGateway.Tests.Auth;

public sealed class McpOAuthAuthenticationTests
{
    [Fact]
    public async Task AddMcpOAuthAuthentication_PreservesBearerAuthenticationAndUsesMcpChallengesAsync()
    {
        using var services = CreateServices();

        var schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();

        (await schemeProvider.GetDefaultAuthenticateSchemeAsync())!.Name
            .Should().Be(JwtBearerDefaults.AuthenticationScheme);
        (await schemeProvider.GetDefaultChallengeSchemeAsync())!.Name
            .Should().Be(McpAuthenticationDefaults.AuthenticationScheme);
    }

    [Fact]
    public async Task ProtectedResourceMetadataRequest_ReturnsExactConfiguredHttpsResourceAsync()
    {
        await using var services = CreateServices();
        var context = CreateContext(
            services,
            "/.well-known/oauth-protected-resource/mcp");

        var handled = await HandleAuthenticationRequestAsync(context);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Body.Position = 0;
        var document = await JsonNode.ParseAsync(context.Response.Body);
        document!["resource"]!.GetValue<string>().Should().Be("https://thetea.app/mcp");
        document["authorization_servers"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Equal("https://auth.thetea.app/realms/dkh");
        document["scopes_supported"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Equal(McpOAuthAuthentication.ToolsScope);
        document["bearer_methods_supported"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Equal("header");
    }

    [Theory]
    [InlineData("/mcp")]
    [InlineData("/mcp/sse")]
    [InlineData("/mcp/message")]
    public async Task Challenge_AdvertisesOneCanonicalMetadataUriForEveryTransportRouteAsync(string path)
    {
        await using var services = CreateServices();
        var context = CreateContext(services, path);

        await context.ChallengeAsync();

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers.WWWAuthenticate.ToString().Should().Be(
            "Bearer resource_metadata=\"https://thetea.app/.well-known/oauth-protected-resource/mcp\"");
    }

    [Fact]
    public async Task ProtectedResourceMetadata_TrustsConfiguredForwardedHostFromKnownProxyAsync()
    {
        await using var services = CreateServices();
        var context = CreateContext(
            services,
            "/.well-known/oauth-protected-resource/mcp",
            Uri.UriSchemeHttp,
            new HostString("dkh-mcp-gateway", 5013));
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.10.10.200");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        context.Request.Headers["X-Forwarded-Proto"] = Uri.UriSchemeHttps;
        context.Request.Headers["X-Forwarded-Host"] = "thetea.app";

        var handled = false;
        var middleware = new ForwardedHeadersMiddleware(
            async httpContext => handled = await HandleAuthenticationRequestAsync(httpContext),
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<IOptions<ForwardedHeadersOptions>>());

        await middleware.Invoke(context);

        handled.Should().BeTrue();
        context.Response.Body.Position = 0;
        var document = await JsonNode.ParseAsync(context.Response.Body);
        document!["resource"]!.GetValue<string>().Should().Be("https://thetea.app/mcp");
    }

    [Theory]
    [InlineData("10.10.10.201", "https", "thetea.app")]
    [InlineData("10.10.10.200", "https", "attacker.example")]
    [InlineData("10.10.10.200", "http", "thetea.app")]
    public async Task ProtectedResourceMetadata_RejectsUnknownProxyHostOrSchemeSpoofAsync(
        string remoteAddress,
        string forwardedScheme,
        string forwardedHost)
    {
        await using var services = CreateServices();
        var context = CreateContext(
            services,
            "/.well-known/oauth-protected-resource/mcp",
            Uri.UriSchemeHttp,
            new HostString("dkh-mcp-gateway", 5013));
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteAddress);
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        context.Request.Headers["X-Forwarded-Proto"] = forwardedScheme;
        context.Request.Headers["X-Forwarded-Host"] = forwardedHost;

        var handled = false;
        var middleware = new ForwardedHeadersMiddleware(
            async httpContext => handled = await HandleAuthenticationRequestAsync(httpContext),
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<IOptions<ForwardedHeadersOptions>>());

        await middleware.Invoke(context);

        handled.Should().BeFalse();
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task Challenge_IgnoresHostAndSchemeSpoofAndKeepsCanonicalHttpsMetadataUriAsync()
    {
        await using var services = CreateServices();
        var context = CreateContext(
            services,
            "/mcp",
            Uri.UriSchemeHttp,
            new HostString("attacker.example"));

        await context.ChallengeAsync();

        context.Response.Headers.WWWAuthenticate.ToString().Should().Be(
            "Bearer resource_metadata=\"https://thetea.app/.well-known/oauth-protected-resource/mcp\"");
    }

    [Fact]
    public void AddMcpOAuthAuthentication_FallsBackToInternalAuthServerUrl()
    {
        var configuration = CreateConfiguration(externalAuthServerUrl: null);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.AddMcpOAuthAuthentication(configuration);

        using var services = serviceCollection.BuildServiceProvider();
        var options = services.GetRequiredService<IOptionsMonitor<McpAuthenticationOptions>>()
            .Get(McpAuthenticationDefaults.AuthenticationScheme);
        options.ResourceMetadata!.AuthorizationServers.Should().Equal("http://localhost:8080/realms/dkh");
        options.ResourceMetadata.Resource.Should().Be("https://thetea.app/mcp");
        options.ResourceMetadataUri.Should().Be(
            new Uri("https://thetea.app/.well-known/oauth-protected-resource/mcp"));
    }

    [Fact]
    public void AddMcpOAuthAuthentication_AllowsExactLoopbackHttpEndpointForLocalDevelopment()
    {
        var configuration = CreateConfiguration(
            externalAuthServerUrl: null,
            publicEndpoint: "http://localhost:5013/mcp");
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.AddMcpOAuthAuthentication(configuration);

        using var services = serviceCollection.BuildServiceProvider();
        var options = services.GetRequiredService<IOptionsMonitor<McpAuthenticationOptions>>()
            .Get(McpAuthenticationDefaults.AuthenticationScheme);
        options.ResourceMetadata!.Resource.Should().Be("http://localhost:5013/mcp");
        options.ResourceMetadataUri.Should().Be(
            new Uri("http://localhost:5013/.well-known/oauth-protected-resource/mcp"));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("https://user:pass@auth.thetea.app")]
    [InlineData("https://auth.thetea.app?tenant=dkh")]
    [InlineData("http://auth.thetea.app")]
    public void AddMcpOAuthAuthentication_InvalidExternalAuthServerUrl_FailsClosed(string value)
    {
        var configuration = CreateConfiguration(value);
        var services = new ServiceCollection();

        var action = () => services.AddMcpOAuthAuthentication(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("https://user:pass@thetea.app/mcp")]
    [InlineData("https://thetea.app/mcp?tenant=dkh")]
    [InlineData("http://thetea.app/mcp")]
    [InlineData("https://thetea.app/mcp/")]
    [InlineData("https://thetea.app/other")]
    public void AddMcpOAuthAuthentication_InvalidPublicEndpoint_FailsClosed(string value)
    {
        var configuration = CreateConfiguration("https://auth.thetea.app", value);
        var services = new ServiceCollection();

        var action = () => services.AddMcpOAuthAuthentication(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    private static ServiceProvider CreateServices()
    {
        var configuration = CreateConfiguration("https://auth.thetea.app");
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddPlatformForwardedHeaders(configuration);
        services.AddMcpOAuthAuthentication(configuration);
        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration(
        string? externalAuthServerUrl,
        string publicEndpoint = "https://thetea.app/mcp")
    {
        var values = new Dictionary<string, string?>
        {
            ["Platform:Auth:Keycloak:AuthServerUrl"] = "http://localhost:8080",
            ["Platform:Auth:Keycloak:Realm"] = "dkh",
            ["Platform:Network:KnownProxies:0"] = "10.10.10.200",
            [McpOAuthAuthentication.PublicEndpointKey] = publicEndpoint
        };
        if (externalAuthServerUrl is not null)
        {
            values["Platform:Auth:Keycloak:ExternalAuthServerUrl"] = externalAuthServerUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static DefaultHttpContext CreateContext(
        IServiceProvider services,
        string path,
        string scheme = "https",
        HostString host = default)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Scheme = scheme;
        context.Request.Host = host.HasValue ? host : new HostString("thetea.app");
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<bool> HandleAuthenticationRequestAsync(HttpContext context)
    {
        var handlerProvider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await handlerProvider.GetHandlerAsync(
            context,
            McpAuthenticationDefaults.AuthenticationScheme);

        return await ((IAuthenticationRequestHandler)handler!).HandleRequestAsync();
    }
}
