using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DKH.McpGateway.Application;

/// <summary>
/// Application layer service registration.
/// </summary>
public static class ConfigureServices
{
    /// <summary>
    /// Registers application services including memory cache for API key validation.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<IApiKeyContext, HttpApiKeyContext>();
        return services;
    }

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

    /// <summary>
    /// Adds API key authentication and usage recording middleware.
    /// Must be called before <c>MapMcp()</c> in the HTTP pipeline.
    /// </summary>
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiKeyAuthMiddleware>();
        app.UseMiddleware<ApiKeyUsageRecorder>();
        return app;
    }
}
