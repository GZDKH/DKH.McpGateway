using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Splits ingress authentication from per-surface authorization for the HTTP MCP transport
/// (issue #86). Ingress admits any valid MCP-eligible key; these request filters then keep a
/// storefront-scoped key on the public <c>storefront_*</c> tool surface and reserve every admin/generic
/// surface — admin tools, prompts, and resources — for admin-authorized callers (MCP key + bearer
/// role). Discovery hides what a caller may not use, and invocation denies the same set, so a hidden
/// primitive can never be reached by name (fail-closed).
/// </summary>
public static class McpSurfaceRequestFilters
{
    /// <summary>
    /// Adds the per-surface list/invoke authorization filters. Call after
    /// <c>AddAuthorizationFilters()</c> so attribute-based policies still apply; this layer adds the
    /// storefront/admin surface split on top.
    /// </summary>
    public static IMcpServerBuilder WithSurfaceAuthorization(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                var guard = context.Services!.GetRequiredService<McpSurfaceGuard>();
                var adminAuthorized = await IsAdminAuthorizedAsync(context.Services!, context.User);
                if (result.Tools.Count > 0)
                {
                    result.Tools = [.. result.Tools.Where(tool => guard.CanAccessTool(tool.Name, adminAuthorized))];
                }

                return result;
            });

            filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var toolName = context.Params?.Name;
                var guard = context.Services!.GetRequiredService<McpSurfaceGuard>();
                var adminAuthorized = await IsAdminAuthorizedAsync(context.Services!, context.User);
                if (string.IsNullOrEmpty(toolName) || !guard.CanAccessTool(toolName, adminAuthorized))
                {
                    throw new McpException(
                        $"Tool '{toolName}' is not available for the presented credential.");
                }

                return await next(context, cancellationToken);
            });

            // Prompts and resources are the admin/generic surface: hidden and denied to
            // storefront-scoped keys and to MCP keys without a privileged bearer.
            filters.AddListPromptsFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                if (!await IsAdminAuthorizedAsync(context.Services!, context.User))
                {
                    result.Prompts = [];
                }

                return result;
            });

            filters.AddGetPromptFilter(next => async (context, cancellationToken) =>
            {
                await DenyUnlessAdminAsync(context.Services!, context.User, "prompt");
                return await next(context, cancellationToken);
            });

            filters.AddListResourcesFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                if (!await IsAdminAuthorizedAsync(context.Services!, context.User))
                {
                    result.Resources = [];
                }

                return result;
            });

            filters.AddListResourceTemplatesFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                if (!await IsAdminAuthorizedAsync(context.Services!, context.User))
                {
                    result.ResourceTemplates = [];
                }

                return result;
            });

            filters.AddReadResourceFilter(next => async (context, cancellationToken) =>
            {
                await DenyUnlessAdminAsync(context.Services!, context.User, "resource");
                return await next(context, cancellationToken);
            });
        });
    }

    private static async ValueTask DenyUnlessAdminAsync(
        IServiceProvider services, ClaimsPrincipal? user, string surface)
    {
        if (!await IsAdminAuthorizedAsync(services, user))
        {
            throw new McpException(
                $"This {surface} is not available for the presented credential.");
        }
    }

    private static async ValueTask<bool> IsAdminAuthorizedAsync(
        IServiceProvider services, ClaimsPrincipal? user)
    {
        var guard = services.GetRequiredService<McpSurfaceGuard>();
        return guard.IsAdminAuthorized(await BearerSatisfiesMcpAccessAsync(services, user));
    }

    private static async ValueTask<bool> BearerSatisfiesMcpAccessAsync(
        IServiceProvider services, ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var authorizationService = services.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(user, McpAuthorizationPolicies.McpAccess);
        return result.Succeeded;
    }
}
