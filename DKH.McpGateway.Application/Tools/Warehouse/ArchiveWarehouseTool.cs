using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class ArchiveWarehouseTool
{
    [McpServerTool(Name = "archive_warehouse"), Description(
        "Archive a warehouse. An archived warehouse is no longer active and cannot receive new operations.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Warehouse ID (GUID)")] string id,
        [Description("Reason for archiving (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new ArchiveWarehouseRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            request.Reason = reason;
        }

        await client.ArchiveWarehouseAsync(request, cancellationToken: cancellationToken);

        return McpProtoHelper.FormatOk();
    }
}
