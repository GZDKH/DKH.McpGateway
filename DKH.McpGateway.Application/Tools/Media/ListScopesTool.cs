using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class ListScopesTool
{
    [McpServerTool(Name = "media_list_scopes"), Description(
        "List all registered media scopes with their allowed roles, MIME types, and max size.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ScopeRegistryService.ScopeRegistryServiceClient client,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListScopesAsync(
            new ListScopesRequest(),
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            scopes = response.Scopes.Select(s => new
            {
                scope = s.Scope,
                container = s.Container,
                roles = s.Roles,
                mimeTypes = s.MimeTypes,
                maxSizeBytes = s.MaxSizeBytes,
            }),
        }, McpJsonDefaults.Options);
    }
}
