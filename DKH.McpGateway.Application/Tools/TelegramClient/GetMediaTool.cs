using DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1;
using MessageCollectionGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1.MessageCollectionService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class GetMediaTool
{
    [McpServerTool(Name = "get_media"), Description("Get Telegram media attachment metadata.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        MessageCollectionGrpc.MessageCollectionServiceClient client,
        [Description("Media attachment ID")] string mediaId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!TelegramClientToolInput.Required(mediaId, nameof(mediaId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetMediaAsync(
            new GetMediaRequest { MediaId = TelegramClientToolInput.ToGuid(mediaId) },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(TelegramClientMapper.MapMedia(response));
    }
}
