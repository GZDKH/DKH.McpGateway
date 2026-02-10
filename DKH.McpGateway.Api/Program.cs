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
    })
    .ConfigurePlatformWebApplication(app =>
    {
        if (!useStdio)
        {
            app.MapMcp();
        }
    })
    .AddPlatformLogging()
    .AddPlatformGrpcEndpoints((_, grpc) => grpc.AddMcpGatewayEndpoints())
    .Build()
    .RunAsync();
