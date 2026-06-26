using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class SuspendWarehouseTool
{
    [McpServerTool(Name = "suspend_warehouse"), Description(
        "Suspend a warehouse. A suspended warehouse is temporarily inactive.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Warehouse ID (GUID)")] string id,
        [Description("Reason for suspension (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new SuspendWarehouseRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            request.Reason = reason;
        }

        var warehouse = await client.SuspendWarehouseAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseTool.MapWarehouse(warehouse), McpJsonDefaults.Options);
    }
}
