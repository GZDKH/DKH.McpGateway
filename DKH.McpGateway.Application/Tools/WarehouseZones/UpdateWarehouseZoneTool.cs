using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class UpdateWarehouseZoneTool
{
    [McpServerTool(Name = "update_warehouse_zone"), Description(
        "Update an existing warehouse zone. " +
        "Only provided fields are updated. " +
        "Set clearCapacity=true to remove the stored capacity value.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Zone ID (GUID)")] string id,
        [Description("Display name as JSON: [{\"lang\":\"en\",\"name\":\"...\"}] (optional)")] string? displayName = null,
        [Description("Zone type (Storage, Picking, Packing, Dispatch, Receiving, Staging — optional)")] string? type = null,
        [Description("Status (Active, Suspended, Archived — optional)")] string? status = null,
        [Description("Capacity (optional integer)")] int? capacity = null,
        [Description("Set true to remove stored capacity (optional)")] bool? clearCapacity = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateWarehouseZoneRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
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
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            request.Type = Enum.Parse<WarehouseZoneType>(type, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<WarehouseZoneStatus>(status, ignoreCase: true);
        }

        if (capacity.HasValue)
        {
            request.Capacity = capacity.Value;
        }

        if (clearCapacity.HasValue)
        {
            request.ClearCapacity = clearCapacity.Value;
        }

        var zone = await client.UpdateWarehouseZoneAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseZoneTool.MapZone(zone), McpJsonDefaults.Options);
    }
}
