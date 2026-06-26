using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class SuspendWarehouseZoneTool
{
    [McpServerTool(Name = "suspend_warehouse_zone"), Description(
        "Suspend a warehouse zone temporarily.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Zone ID (GUID)")] string id,
        [Description("Reason for suspension (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new SuspendWarehouseZoneRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            request.Reason = reason;
        }

        var zone = await client.SuspendWarehouseZoneAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseZoneTool.MapZone(zone), McpJsonDefaults.Options);
    }
}
