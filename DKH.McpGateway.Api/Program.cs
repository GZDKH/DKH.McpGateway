var useStdio = args.Contains("--stdio") ||
               Environment.GetEnvironmentVariable("MCP_TRANSPORT")?.Equals("stdio", StringComparison.OrdinalIgnoreCase) == true;

await Platform
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
            app.MapMcp();
        }

        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");
    })
    .AddPlatformLogging()
    .AddPlatformGrpcEndpoints((_, grpc) => grpc.AddMcpGatewayEndpoints())
    .Build()
    .RunAsync();
