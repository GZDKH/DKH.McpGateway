using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class GetBroadcastTool
{
    [McpServerTool(Name = "get_broadcast"), Description("Get a broadcast by ID. No customer contact PII is returned.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        BroadcastGrpc.BroadcastServiceClient client,
        [Description("Broadcast ID (GUID)")] string broadcastId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            return McpProtoHelper.FormatError("broadcastId is required");
        }

        var model = await client.GetBroadcastAsync(
            new GetBroadcastRequest { Id = new GuidValue(broadcastId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(BroadcastMapper.MapBroadcast(model), McpJsonDefaults.Options);
    }
}
