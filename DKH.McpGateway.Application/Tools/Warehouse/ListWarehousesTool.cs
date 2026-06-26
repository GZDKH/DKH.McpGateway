using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class ListWarehousesTool
{
    [McpServerTool(Name = "list_warehouses"), Description(
        "List warehouses with optional filtering by ownership type, status, search term, or public pickup point flag. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Ownership type filter (Own, ThreePl, Supplier, Dropship — optional)")] string? ownershipType = null,
        [Description("Status filter (Active, Suspended, Archived — optional)")] string? status = null,
        [Description("Search term (optional)")] string? search = null,
        [Description("Filter by public pickup point flag (optional)")] bool? isPublicPickupPoint = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListWarehousesRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(ownershipType))
        {
            request.OwnershipType = Enum.Parse<WarehouseOwnershipType>(ownershipType, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<WarehouseStatus>(status, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            request.Search = search;
        }

        if (isPublicPickupPoint.HasValue)
        {
            request.IsPublicPickupPoint = isPublicPickupPoint.Value;
        }

        var response = await client.ListWarehousesAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetWarehouseTool.MapWarehouse),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
