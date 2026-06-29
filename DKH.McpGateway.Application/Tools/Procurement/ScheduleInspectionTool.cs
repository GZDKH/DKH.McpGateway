using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class ScheduleInspectionTool
{
    [McpServerTool(Name = "schedule_inspection"), Description(
        "Schedule a new inspection for a purchase order. " +
        "Assigns an inspector, sets scheduled time, sample size, and inspection type. " +
        "Valid inspection types: Unspecified, PreShipment, InProcess, Final.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Purchase order ID (GUID)")] string purchaseOrderId,
        [Description("Inspector reference ID (GUID)")] string inspectorId,
        [Description("Scheduled date/time (ISO 8601)")] string scheduledAt,
        [Description("Number of samples to inspect")] int sampleSize,
        [Description("Inspection type (Unspecified, PreShipment, InProcess, Final)")] string inspectionType,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(purchaseOrderId))
        {
            return McpProtoHelper.FormatError("purchaseOrderId is required");
        }

        if (string.IsNullOrWhiteSpace(inspectorId))
        {
            return McpProtoHelper.FormatError("inspectorId is required");
        }

        if (string.IsNullOrWhiteSpace(scheduledAt))
        {
            return McpProtoHelper.FormatError("scheduledAt is required");
        }

        if (string.IsNullOrWhiteSpace(inspectionType))
        {
            return McpProtoHelper.FormatError("inspectionType is required");
        }

        var inspection = await client.ScheduleInspectionAsync(
            new ScheduleInspectionRequest
            {
                PurchaseOrderId = new GuidValue(purchaseOrderId),
                InspectorRef = new GuidValue(inspectorId),
                ScheduledAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.Parse(scheduledAt, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime()),
                SampleSize = sampleSize,
                InspectionType = Enum.Parse<InspectionType>(inspectionType, ignoreCase: true),
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetInspectionTool.MapInspection(inspection), McpJsonDefaults.Options);
    }
}
