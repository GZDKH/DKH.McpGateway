using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetInspectionTool
{
    [McpServerTool(Name = "get_inspection"), Description(
        "Get a single inspection by its ID. " +
        "Returns inspection status, scheduling, sample results, decision, and evidence asset IDs.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspection ID (GUID)")] string inspectionId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(inspectionId))
        {
            return McpProtoHelper.FormatError("inspectionId is required");
        }

        var inspection = await client.GetInspectionAsync(
            new GetInspectionRequest { InspectionId = new GuidValue(inspectionId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapInspection(inspection), McpJsonDefaults.Options);
    }

    internal static object MapInspection(InspectionModel i) => new
    {
        id = i.Id?.Value,
        purchaseOrderId = i.PurchaseOrderId?.Value,
        inspectorRef = i.InspectorRef?.Value,
        scheduledAt = i.ScheduledAt?.ToDateTimeOffset().ToString("O"),
        startedAt = i.StartedAt?.ToDateTimeOffset().ToString("O"),
        completedAt = i.CompletedAt?.ToDateTimeOffset().ToString("O"),
        cancelledAt = i.CancelledAt?.ToDateTimeOffset().ToString("O"),
        cancellationReason = i.CancellationReason,
        status = i.Status.ToString(),
        sampleSize = i.SampleSize,
        decision = i.Decision.ToString(),
        type = i.Type.ToString(),
        completedByName = i.CompletedByName,
        completedByEmployeeId = i.CompletedByEmployeeId?.Value,
        photoEvidenceAssetIds = i.PhotoEvidenceAssetIds.Select(g => g.Value),
        sampleResults = i.SampleResults.Select(MapSampleResult),
    };

    internal static object MapSampleResult(SampleResultModel r) => new
    {
        index = r.Index,
        passed = r.Passed,
        recordedAt = r.RecordedAt?.ToDateTimeOffset().ToString("O"),
        notes = r.Notes,
    };
}
