using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class GetAssetTool
{
    [McpServerTool(Name = "media_get_asset"), Description(
        "Get a media asset's storage reference by asset ID. " +
        "Returns container, key, content type and size. No PII.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AssetService.AssetServiceClient client,
        [Description("Asset ID (GUID)")] string assetId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(assetId))
        {
            return McpProtoHelper.FormatError("assetId is required");
        }

        var model = await client.GetAssetAsync(
            new GetAssetRequest { AssetId = assetId },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MediaMappers.MapMediaRef(model), McpJsonDefaults.Options);
    }
}
