using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class CreateWarehouseZoneTool
{
    [McpServerTool(Name = "create_warehouse_zone"), Description(
        "Create a new zone within a warehouse. " +
        "displayName is a JSON array of {\"lang\":\"en\",\"name\":\"...\"} objects.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Warehouse ID (GUID)")] string warehouseId,
        [Description("Zone code (unique short identifier)")] string code,
        [Description("Display name as JSON: [{\"lang\":\"en\",\"name\":\"...\"}]")] string displayName,
        [Description("Zone type (Storage, Picking, Packing, Dispatch, Receiving, Staging)")] string type,
        [Description("Capacity (optional integer)")] int? capacity = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return McpProtoHelper.FormatError("displayName is required");
        }

        var request = new CreateWarehouseZoneRequest
        {
            WarehouseId = new GuidValue(warehouseId),
            Code = code,
            Type = Enum.Parse<WarehouseZoneType>(type, ignoreCase: true),
        };

        try
        {
            var entries = JsonSerializer.Deserialize<List<JsonElement>>(displayName, McpJsonDefaults.Options);
            var localizedText = new WarehouseLocalizedText();
            if (entries is not null)
            {
                foreach (var entry in entries)
                {
                    var lang = entry.GetProperty("lang").GetString() ?? "";
                    var name = entry.GetProperty("name").GetString() ?? "";
                    localizedText.Values[lang] = name;
                }
            }

            request.DisplayName = localizedText;
        }
        catch (Exception)
        {
            return McpProtoHelper.FormatError("displayName JSON is invalid");
        }

        if (capacity.HasValue)
        {
            request.Capacity = capacity.Value;
        }

        var zone = await client.CreateWarehouseZoneAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseZoneTool.MapZone(zone), McpJsonDefaults.Options);
    }
}
