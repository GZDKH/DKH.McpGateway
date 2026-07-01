using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using DKH.BroadcastService.Contracts.Broadcast.Models.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class ListBroadcastsTool
{
    [McpServerTool(Name = "list_broadcasts"), Description(
        "List broadcasts with optional storefront, target type and status filters. No customer contact PII is returned.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        BroadcastGrpc.BroadcastServiceClient client,
        [Description("Storefront ID (GUID, optional)")] string? storefrontId = null,
        [Description("Delivery channel target type, e.g. telegram (optional)")] string? targetType = null,
        [Description("Filter by status: Pending, Executed, Cancelled, Failed (optional)")] string? status = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListBroadcastsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(storefrontId))
        {
            request.StorefrontId = new GuidValue(storefrontId);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            request.TargetType = targetType;
        }

        var parsedStatus = BroadcastInput.ParseStatus(status);
        if (parsedStatus != BroadcastStatus.Unspecified)
        {
            request.Status = parsedStatus;
        }

        var response = await client.ListBroadcastsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(BroadcastMapper.MapBroadcast),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
