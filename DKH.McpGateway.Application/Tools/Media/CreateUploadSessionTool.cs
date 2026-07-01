using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class CreateUploadSessionTool
{
    [McpServerTool(Name = "media_create_upload_session"), Description(
        "Create an upload session for a scope + scope key, returning a signed upload URL. " +
        "Requester identity omitted from the response.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        UploadSessionService.UploadSessionServiceClient client,
        [Description("Attachment scope (e.g. product, category)")] string scope,
        [Description("Scope key (entity ID the upload will attach to)")] string scopeKey,
        [Description("Upload sub-kind (optional)")] string? subKind = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(scope))
        {
            return McpProtoHelper.FormatError("scope is required");
        }

        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            return McpProtoHelper.FormatError("scopeKey is required");
        }

        var session = await client.CreateUploadSessionAsync(
            new CreateUploadSessionRequest
            {
                Scope = scope,
                ScopeKey = scopeKey,
                SubKind = subKind ?? "",
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MediaMappers.MapUploadSession(session), McpJsonDefaults.Options);
    }
}
