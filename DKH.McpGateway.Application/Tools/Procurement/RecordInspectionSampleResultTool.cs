using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class RecordInspectionSampleResultTool
{
    [McpServerTool(Name = "record_inspection_sample_result"), Description(
        "Record the result of a single sample during an in-progress inspection. " +
        "Specify the sample index (0-based), whether it passed, and optional notes.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspection ID (GUID)")] string inspectionId,
        [Description("Sample index (0-based)")] int index,
        [Description("Whether this sample passed inspection")] bool passed,
        [Description("Notes about this sample result (optional)")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(inspectionId))
        {
            return McpProtoHelper.FormatError("inspectionId is required");
        }

        var inspection = await client.RecordInspectionSampleResultAsync(
            new RecordInspectionSampleResultRequest
            {
                InspectionId = new GuidValue(inspectionId),
                Index = index,
                Passed = passed,
                Notes = notes ?? string.Empty,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetInspectionTool.MapInspection(inspection), McpJsonDefaults.Options);
    }
}
