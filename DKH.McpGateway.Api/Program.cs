using DKH.Platform.Authentication.Keycloak;
using DKH.Platform.Authorization;
using DKH.Platform.Identity;
using DKH.Platform.Telemetry;

var useStdio = args.Contains("--stdio") ||
               Environment.GetEnvironmentVariable("MCP_TRANSPORT")?.Equals("stdio", StringComparison.OrdinalIgnoreCase) == true;

var platform = Platform
    .CreateWeb(args)
    .ConfigurePlatformWebApplicationBuilder(builder =>
    {
        var mcp = builder.Services.AddMcpGatewayServer();

        if (useStdio)
        {
            mcp.WithStdioServerTransport();
        }
        else
        {
            mcp.WithHttpTransport();
        }

        builder.Services.AddApplication();
        builder.Services.AddHealthChecks();
    })
    .ConfigurePlatformWebApplication(app =>
    {
        if (!useStdio)
        {
            app.UseApiKeyAuth();
            app.MapMcp().RequireAuthorization(McpAuthorizationPolicies.McpAccess);
        }

        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");
    })
    .AddPlatformLogging()
    .AddPlatformTelemetry()
    .AddPlatformGrpcEndpoints((_, grpc) => grpc.AddMcpGatewayEndpoints());

if (!useStdio)
{
    platform = platform
        .AddPlatformKeycloakAuth()
        .AddHttpCurrentUser()
        .AddPlatformAuthorization(policies => policies.AddRolePolicy(
            McpAuthorizationPolicies.McpAccess,
            PlatformRoles.Realm.SuperAdmin,
            PlatformRoles.Realm.Admin,
            PlatformRoles.FullAccess));
}

await platform.Build().RunAsync();

internal static class McpAuthorizationPolicies
{
    public const string McpAccess = "McpAccess";
}
