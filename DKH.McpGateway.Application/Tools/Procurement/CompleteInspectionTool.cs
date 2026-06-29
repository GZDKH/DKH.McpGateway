using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class CompleteInspectionTool
{
    [McpServerTool(Name = "complete_inspection"), Description(
        "Complete an in-progress inspection with a final decision. " +
        "Valid decisions: Unspecified, Passed, Failed, ConditionalPass. " +
        "Optionally attach photo evidence asset IDs (comma-separated GUIDs), " +
        "inspector name, and employee ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspection ID (GUID)")] string inspectionId,
        [Description("Final decision (Unspecified, Passed, Failed, ConditionalPass)")] string decision,
        [Description("Completed-by inspector name (optional)")] string? completedByName = null,
        [Description("Completed-by employee ID (GUID, optional)")] string? completedByEmployeeId = null,
        [Description("Comma-separated photo evidence asset IDs (GUIDs, optional)")] string? photoEvidenceAssetIds = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(inspectionId))
        {
            return McpProtoHelper.FormatError("inspectionId is required");
        }

        if (string.IsNullOrWhiteSpace(decision))
        {
            return McpProtoHelper.FormatError("decision is required");
        }

        var request = new CompleteInspectionRequest
        {
            InspectionId = new GuidValue(inspectionId),
            Decision = Enum.Parse<InspectionDecision>(decision, ignoreCase: true),
            CompletedByName = completedByName ?? string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(completedByEmployeeId))
        {
            request.CompletedByEmployeeId = new GuidValue(completedByEmployeeId);
        }

        if (!string.IsNullOrWhiteSpace(photoEvidenceAssetIds))
        {
            foreach (var raw in photoEvidenceAssetIds.Split(',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.PhotoEvidenceAssetIds.Add(new GuidValue(raw));
            }
        }

        var inspection = await client.CompleteInspectionAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetInspectionTool.MapInspection(inspection), McpJsonDefaults.Options);
    }
}
