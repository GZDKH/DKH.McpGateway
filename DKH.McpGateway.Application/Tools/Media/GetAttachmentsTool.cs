using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class GetAttachmentsTool
{
    [McpServerTool(Name = "media_get_attachments"), Description(
        "Get media attachments for a scope + scope key, optionally filtered by role. " +
        "Returns storage reference, sort order, alt text and caption. Actor identity omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AttachmentService.AttachmentServiceClient client,
        [Description("Attachment scope (e.g. product, category)")] string scope,
        [Description("Scope key (entity ID the attachments belong to)")] string scopeKey,
        [Description("Comma-separated roles to filter by (optional)")] string? roles = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(scope))
        {
            return McpProtoHelper.FormatError("scope is required");
        }

        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            return McpProtoHelper.FormatError("scopeKey is required");
        }

        var request = new GetAttachmentsRequest { Scope = scope, ScopeKey = scopeKey };

        if (!string.IsNullOrWhiteSpace(roles))
        {
            foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.Roles.Add(role);
            }
        }

        var response = await client.GetAttachmentsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            attachments = response.Attachments.Select(MediaMappers.MapAttachment),
        }, McpJsonDefaults.Options);
    }
}
