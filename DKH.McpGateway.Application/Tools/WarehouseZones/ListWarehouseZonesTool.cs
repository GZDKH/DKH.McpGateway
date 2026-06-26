using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class ListWarehouseZonesTool
{
    [McpServerTool(Name = "list_warehouse_zones"), Description(
        "List warehouse zones for a given warehouse. " +
        "warehouseId is required to scope the results. " +
        "Supports optional filtering by type, status, or search term.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Warehouse ID (GUID, required — scopes zones to one warehouse)")] string warehouseId,
        [Description("Zone type filter (Storage, Picking, Packing, Dispatch, Receiving, Staging — optional)")] string? type = null,
        [Description("Status filter (Active, Suspended, Archived — optional)")] string? status = null,
        [Description("Search term (optional)")] string? search = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required");
        }

        var request = new ListWarehouseZonesRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
            WarehouseId = new GuidValue(warehouseId),
        };

        if (!string.IsNullOrWhiteSpace(type))
        {
            request.Type = Enum.Parse<WarehouseZoneType>(type, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<WarehouseZoneStatus>(status, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            request.Search = search;
        }

        var response = await client.ListWarehouseZonesAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetWarehouseZoneTool.MapZone),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
