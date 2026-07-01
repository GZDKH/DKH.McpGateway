using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class RetryBroadcastTool
{
    [McpServerTool(Name = "retry_broadcast"), Description("Retry a failed broadcast.")]
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

        var model = await client.RetryBroadcastAsync(
            new RetryBroadcastRequest { Id = new GuidValue(broadcastId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(BroadcastMapper.MapBroadcast(model), McpJsonDefaults.Options);
    }
}
