using DKH.McpGateway.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DKH.McpGateway.Tests.Auth;

public sealed class McpGatewayEndpointRoutingTests
{
    [Fact]
    public void MapMcpGateway_MapsOnlyCanonicalPrefixAndProtectsEveryRoute()
    {
        var builder = WebApplication.CreateBuilder();
#pragma warning disable MCP9004 // Test the intentionally retained trusted-client legacy routes.
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => options.EnableLegacySse = true);
#pragma warning restore MCP9004
        builder.Services.AddAuthorization();
        var app = builder.Build();

        app.MapMcpGateway();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();
        endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Distinct()
            .Should().BeEquivalentTo("/mcp/", "/mcp/sse", "/mcp/message");
        endpoints.Should().OnlyContain(endpoint =>
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Any(data => data.Policy == McpAuthorizationPolicies.McpAccess));
    }
}
