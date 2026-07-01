using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class GetAssetDownloadLinkTool
{
    [McpServerTool(Name = "media_get_asset_download_link"), Description(
        "Get a signed, time-limited download link for a media asset. " +
        "Optionally set a TTL and force a Content-Disposition: attachment response.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AssetService.AssetServiceClient client,
        [Description("Asset ID (GUID)")] string assetId,
        [Description("Link time-to-live in seconds (optional, 0 = service default)")] int? ttlSeconds = null,
        [Description("Force download via Content-Disposition: attachment (optional)")] bool? forceDownload = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(assetId))
        {
            return McpProtoHelper.FormatError("assetId is required");
        }

        var response = await client.GetAssetDownloadLinkAsync(
            new GetAssetDownloadLinkRequest
            {
                AssetId = assetId,
                TtlSeconds = ttlSeconds ?? 0,
                ForceDownload = forceDownload ?? false,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            assetId = response.AssetId,
            url = response.Url,
            expiresAtUtc = response.ExpiresAtUtc?.ToDateTimeOffset().ToString("O"),
            contentType = response.ContentType,
            lengthBytes = response.LengthBytes,
        }, McpJsonDefaults.Options);
    }
}
