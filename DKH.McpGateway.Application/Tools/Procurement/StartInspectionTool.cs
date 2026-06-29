using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class StartInspectionTool
{
    [McpServerTool(Name = "start_inspection"), Description(
        "Start a scheduled inspection, transitioning its status to InProgress.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspection ID (GUID)")] string inspectionId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(inspectionId))
        {
            return McpProtoHelper.FormatError("inspectionId is required");
        }

        var inspection = await client.StartInspectionAsync(
            new StartInspectionRequest { InspectionId = new GuidValue(inspectionId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetInspectionTool.MapInspection(inspection), McpJsonDefaults.Options);
    }
}
