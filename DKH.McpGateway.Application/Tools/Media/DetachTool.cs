using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class DetachTool
{
    [McpServerTool(Name = "media_detach"), Description(
        "Detach (remove) an attachment. Destructive — the attachment link is permanently removed.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AttachmentService.AttachmentServiceClient client,
        [Description("Attachment ID (GUID)")] string attachmentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(attachmentId))
        {
            return McpProtoHelper.FormatError("attachmentId is required");
        }

        await client.DetachAsync(
            new DetachRequest { AttachmentId = attachmentId },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { ok = true }, McpJsonDefaults.Options);
    }
}
