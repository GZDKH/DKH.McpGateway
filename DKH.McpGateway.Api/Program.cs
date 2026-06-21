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
            // MCP SDK 1.2+ disables the legacy SSE endpoints (/sse, /message) by default.
            // Keep them enabled to preserve the documented SSE transport alongside Streamable HTTP.
            // EnableLegacySse is obsolete (MCP9004); intentional here — the gateway is auth-gated
            // (API key + Keycloak), satisfying the "trusted clients only" caveat. Dropping legacy SSE
            // in favour of Streamable HTTP only is tracked as a follow-up on issue #70.
#pragma warning disable MCP9004 // Legacy SSE transport is obsolete; kept for backward-compatible clients.
            mcp.WithHttpTransport(static options => options.EnableLegacySse = true);
#pragma warning restore MCP9004
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
