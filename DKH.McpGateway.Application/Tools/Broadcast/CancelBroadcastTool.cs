using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class CancelBroadcastTool
{
    [McpServerTool(Name = "cancel_broadcast"), Description("Cancel a pending broadcast.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        BroadcastGrpc.BroadcastServiceClient client,
        [Description("Broadcast ID (GUID)")] string broadcastId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            return McpProtoHelper.FormatError("broadcastId is required");
        }

        var model = await client.CancelBroadcastAsync(
            new CancelBroadcastRequest { Id = new GuidValue(broadcastId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(BroadcastMapper.MapBroadcast(model), McpJsonDefaults.Options);
    }
}
