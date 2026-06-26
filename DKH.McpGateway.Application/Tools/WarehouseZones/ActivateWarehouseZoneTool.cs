using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class ActivateWarehouseZoneTool
{
    [McpServerTool(Name = "activate_warehouse_zone"), Description(
        "Activate a previously suspended warehouse zone.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Zone ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var zone = await client.ActivateWarehouseZoneAsync(
            new ActivateWarehouseZoneRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseZoneTool.MapZone(zone), McpJsonDefaults.Options);
    }
}
