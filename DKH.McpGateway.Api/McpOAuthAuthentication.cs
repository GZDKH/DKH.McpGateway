using DKH.Platform.Authentication.Keycloak;
using Microsoft.AspNetCore.Authentication;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace DKH.McpGateway.Api;

internal static class McpOAuthAuthentication
{
    internal const string ToolsScope = "mcp:tools";
    internal const string PublicEndpointKey = "Mcp:PublicEndpoint";

    internal static IServiceCollection AddMcpOAuthAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var authorizationServer = GetExternalAuthorizationServer(configuration);
        var publicEndpoint = GetPublicEndpoint(configuration);
        var resourceMetadataUri = GetResourceMetadataUri(publicEndpoint);

        services
            .AddAuthentication()
            .AddMcp(options =>
            {
                // One canonical resource prevents Host/scheme spoofing and keeps the
                // retained legacy SSE routes on the same OAuth audience as /mcp.
                options.ResourceMetadataUri = resourceMetadataUri;
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = publicEndpoint.AbsoluteUri,
                    AuthorizationServers = [authorizationServer],
                    BearerMethodsSupported = ["header"],
                    // Keycloak uses this optional client scope to attach the canonical
                    // audience. Realm roles and API-key permissions remain the access gates.
                    ScopesSupported = [ToolsScope],
                    ResourceName = "DKH MCP Gateway"
                };
            });

        // JWT Bearer remains the default authenticate scheme (registered by
        // AddPlatformKeycloakAuth). MCP only owns challenges and RFC 9728 metadata.
        services.PostConfigure<AuthenticationOptions>(options =>
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme);

        return services;
    }

    private static Uri GetPublicEndpoint(IConfiguration configuration)
    {
        var value = configuration[PublicEndpointKey];
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var endpoint) ||
            (!endpoint.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException(
                $"{PublicEndpointKey} must be an absolute HTTP(S) URL without user info, query, or fragment.");
        }

        if (!endpoint.AbsolutePath.Equals(McpGatewayEndpointRouting.HttpEndpoint, StringComparison.Ordinal) ||
            value.EndsWith('/'))
        {
            throw new InvalidOperationException(
                $"{PublicEndpointKey} must end with the exact canonical path {McpGatewayEndpointRouting.HttpEndpoint} without a trailing slash.");
        }

        if (endpoint.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !endpoint.IsLoopback)
        {
            throw new InvalidOperationException(
                $"{PublicEndpointKey} must use HTTPS outside loopback development.");
        }

        return endpoint;
    }

    private static Uri GetResourceMetadataUri(Uri publicEndpoint)
    {
        var builder = new UriBuilder(
            publicEndpoint.Scheme,
            publicEndpoint.Host,
            publicEndpoint.IsDefaultPort ? -1 : publicEndpoint.Port,
            $"/.well-known/oauth-protected-resource{publicEndpoint.AbsolutePath}");
        return builder.Uri;
    }

    private static string GetExternalAuthorizationServer(IConfiguration configuration)
    {
        var keycloak = configuration.GetSection(PlatformKeycloakAuthOptions.Section);
        var baseUrl = keycloak[nameof(PlatformKeycloakAuthOptions.ExternalAuthServerUrl)];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = keycloak[nameof(PlatformKeycloakAuthOptions.AuthServerUrl)];
        }

        var realm = keycloak[nameof(PlatformKeycloakAuthOptions.Realm)];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(realm))
        {
            throw new InvalidOperationException(
                $"{PlatformKeycloakAuthOptions.Section} must define AuthServerUrl and Realm for MCP OAuth metadata.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedBaseUrl) ||
            (!parsedBaseUrl.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !parsedBaseUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrEmpty(parsedBaseUrl.UserInfo) ||
            !string.IsNullOrEmpty(parsedBaseUrl.Query) ||
            !string.IsNullOrEmpty(parsedBaseUrl.Fragment))
        {
            throw new InvalidOperationException(
                $"{PlatformKeycloakAuthOptions.Section}:ExternalAuthServerUrl must be an absolute HTTP(S) URL without user info, query, or fragment.");
        }

        if (parsedBaseUrl.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !parsedBaseUrl.IsLoopback)
        {
            throw new InvalidOperationException(
                $"{PlatformKeycloakAuthOptions.Section}:ExternalAuthServerUrl must use HTTPS outside loopback development.");
        }

        return $"{baseUrl.TrimEnd('/')}/realms/{Uri.EscapeDataString(realm)}";
    }
}
