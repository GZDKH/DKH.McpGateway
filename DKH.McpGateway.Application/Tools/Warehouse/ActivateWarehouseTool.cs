using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class ActivateWarehouseTool
{
    [McpServerTool(Name = "activate_warehouse"), Description(
        "Activate a previously suspended warehouse.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Warehouse ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var warehouse = await client.ActivateWarehouseAsync(
            new ActivateWarehouseRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseTool.MapWarehouse(warehouse), McpJsonDefaults.Options);
    }
}
