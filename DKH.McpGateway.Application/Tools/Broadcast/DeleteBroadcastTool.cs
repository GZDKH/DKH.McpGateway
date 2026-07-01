using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class DeleteBroadcastTool
{
    [McpServerTool(Name = "delete_broadcast"), Description("Delete a broadcast.")]
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

        await client.DeleteBroadcastAsync(
            new DeleteBroadcastRequest { Id = new GuidValue(broadcastId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { deleted = true, id = broadcastId }, McpJsonDefaults.Options);
    }
}
