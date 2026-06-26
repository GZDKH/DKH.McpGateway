using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;

namespace DKH.McpGateway.Application.Tools.WarehouseZones;

[McpServerToolType]
public static class ArchiveWarehouseZoneTool
{
    [McpServerTool(Name = "archive_warehouse_zone"), Description(
        "Archive a warehouse zone. An archived zone is no longer available for operations.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseZoneCrudService.WarehouseZoneCrudServiceClient client,
        [Description("Zone ID (GUID)")] string id,
        [Description("Reason for archiving (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new ArchiveWarehouseZoneRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            request.Reason = reason;
        }

        await client.ArchiveWarehouseZoneAsync(request, cancellationToken: cancellationToken);

        return McpProtoHelper.FormatOk();
    }
}
