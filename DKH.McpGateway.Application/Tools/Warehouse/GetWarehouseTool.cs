using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class GetWarehouseTool
{
    [McpServerTool(Name = "get_warehouse"), Description(
        "Get a single warehouse by its ID. " +
        "Returns warehouse details including address, timezone, status, and ownership type.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Warehouse ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var warehouse = await client.GetWarehouseAsync(
            new GetWarehouseRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapWarehouse(warehouse), McpJsonDefaults.Options);
    }

    internal static object MapWarehouse(WarehouseModel w) => new
    {
        id = w.Id?.Value,
        tenantId = w.TenantId?.Value,
        code = w.Code,
        displayName = w.DisplayName?.Values,
        ownershipType = w.OwnershipType.ToString(),
        operatorCounterpartyId = w.OperatorCounterpartyId?.Value,
        address = w.Address is null ? null : new
        {
            lines = w.Address.Lines,
            city = w.Address.City,
            region = w.Address.Region,
            country = w.Address.Country,
            postalCode = w.Address.PostalCode,
        },
        timezone = w.Timezone,
        status = w.Status.ToString(),
        isPublicPickupPoint = w.IsPublicPickupPoint,
        createdAt = w.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = w.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
