using DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1;
using MessageCollectionGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1.MessageCollectionService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class DownloadMediaTool
{
    [McpServerTool(Name = "download_media"), Description("Download Telegram media to service storage and return metadata.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        MessageCollectionGrpc.MessageCollectionServiceClient client,
        [Description("Media attachment ID")] string mediaId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!TelegramClientToolInput.Required(mediaId, nameof(mediaId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.DownloadMediaAsync(
            new DownloadMediaRequest { MediaId = TelegramClientToolInput.ToGuid(mediaId) },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(TelegramClientMapper.MapMedia(response));
    }
}
