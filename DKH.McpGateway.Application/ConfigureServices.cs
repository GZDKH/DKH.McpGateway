using Microsoft.Extensions.DependencyInjection;

namespace DKH.McpGateway.Application;

/// <summary>
/// Application layer service registration.
/// </summary>
public static class ConfigureServices
{
    /// <summary>
    /// Registers application services.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;

    /// <summary>
    /// Registers the MCP server with tools, resources, and prompts.
    /// Call <c>.WithStdioServerTransport()</c> or <c>.WithHttpTransport()</c> on the result.
    /// </summary>
    public static IMcpServerBuilder AddMcpGatewayServer(this IServiceCollection services)
    {
        return services
            .AddMcpServer(options =>
                options.ServerInfo = new() { Name = "DKH.McpGateway", Version = "1.0.0" })
            .WithToolsFromAssembly(typeof(ConfigureServices).Assembly)
            .WithResourcesFromAssembly(typeof(ConfigureServices).Assembly)
            .WithPromptsFromAssembly(typeof(ConfigureServices).Assembly);
    }
}
