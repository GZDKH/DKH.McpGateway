var useStdio = args.Contains("--stdio") ||
               Environment.GetEnvironmentVariable("MCP_TRANSPORT")?.Equals("stdio", StringComparison.OrdinalIgnoreCase) == true;

if (useStdio)
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services
        .AddMcpServer(options =>
            options.ServerInfo = new() { Name = "DKH.McpGateway", Version = "1.0.0" })
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(ConfigureServices).Assembly);

    var host = builder.Build();
    await host.RunAsync();
}
else
{
    await Platform
        .CreateWeb(args)
        .ConfigurePlatformWebApplicationBuilder(builder =>
        {
            builder.Services
                .AddMcpServer(options =>
                    options.ServerInfo = new() { Name = "DKH.McpGateway", Version = "1.0.0" })
                .WithHttpTransport()
                .WithToolsFromAssembly(typeof(ConfigureServices).Assembly);

            builder.Services.AddApplication();
        })
        .ConfigurePlatformWebApplication(app => app.MapMcp())
        .AddPlatformLogging()
        .AddPlatformGrpcEndpoints((_, grpc) => grpc.AddMcpGatewayEndpoints())
        .Build()
        .RunAsync();
}
