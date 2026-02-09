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
}
