using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class UpdateAttachmentMetadataTool
{
    [McpServerTool(Name = "media_update_attachment_metadata"), Description(
        "Update an attachment's alt text and caption. Pass an empty string to clear a field.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AttachmentService.AttachmentServiceClient client,
        [Description("Attachment ID (GUID)")] string attachmentId,
        [Description("Alt text (optional, empty clears it)")] string? altText = null,
        [Description("Caption (optional, empty clears it)")] string? caption = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(attachmentId))
        {
            return McpProtoHelper.FormatError("attachmentId is required");
        }

        var attachment = await client.UpdateMetadataAsync(
            new UpdateMetadataRequest
            {
                AttachmentId = attachmentId,
                AltText = altText ?? "",
                Caption = caption ?? "",
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MediaMappers.MapAttachment(attachment), McpJsonDefaults.Options);
    }
}
