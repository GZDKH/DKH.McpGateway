using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GenerateInspectionReportTool
{
    [McpServerTool(Name = "generate_inspection_report"), Description(
        "Generate a PDF inspection report for a completed inspection. " +
        "Returns file metadata (fileName, contentType, contentLength in bytes). " +
        "The binary content is intentionally omitted — use the returned metadata to retrieve the file via the asset pipeline.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Inspection ID (GUID)")] string inspectionId,
        [Description("Inspector display name for the report (optional)")] string? inspectorName = null,
        [Description("Purchase order number for the report header (optional)")] string? purchaseOrderNumber = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(inspectionId))
        {
            return McpProtoHelper.FormatError("inspectionId is required");
        }

        var report = await client.GenerateInspectionReportAsync(
            new GenerateInspectionReportRequest
            {
                InspectionId = new GuidValue(inspectionId),
                InspectorName = inspectorName ?? string.Empty,
                PurchaseOrderNumber = purchaseOrderNumber ?? string.Empty,
            },
            cancellationToken: cancellationToken);

        // OMIT report.Content (large base64 PDF payload — PII masking rule: InspectionReportFileModel.content)
        return JsonSerializer.Serialize(new
        {
            fileName = report.FileName,
            contentType = report.ContentType,
            contentLength = report.Content.Length,
        }, McpJsonDefaults.Options);
    }
}
