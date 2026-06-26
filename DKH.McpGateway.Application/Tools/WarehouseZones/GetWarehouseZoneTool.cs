using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class GetWarehouseZoneTool
{
    [McpServerTool(Name = "get_warehouse_zone"), Description(
        "Get a single warehouse zone by its ID. " +
        "Returns zone details including type, status, and optional capacity.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Zone ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var zone = await client.GetWarehouseZoneAsync(
            new GetWarehouseZoneRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapZone(zone), McpJsonDefaults.Options);
    }

    internal static object MapZone(WarehouseZoneModel z) => new
    {
        id = z.Id?.Value,
        warehouseId = z.WarehouseId?.Value,
        code = z.Code,
        displayName = z.DisplayName?.Values,
        type = z.Type.ToString(),
        status = z.Status.ToString(),
        capacity = z.Capacity > 0 ? z.Capacity : null,
        createdAt = z.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = z.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
