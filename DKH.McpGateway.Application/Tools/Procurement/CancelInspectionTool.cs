using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class CancelInspectionTool
{
    [McpServerTool(Name = "cancel_inspection"), Description(
        "Cancel a scheduled or in-progress inspection with a reason.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspection ID (GUID)")] string inspectionId,
        [Description("Cancellation reason (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(inspectionId))
        {
            return McpProtoHelper.FormatError("inspectionId is required");
        }

        var inspection = await client.CancelInspectionAsync(
            new CancelInspectionRequest
            {
                InspectionId = new GuidValue(inspectionId),
                Reason = reason ?? string.Empty,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetInspectionTool.MapInspection(inspection), McpJsonDefaults.Options);
    }
}
