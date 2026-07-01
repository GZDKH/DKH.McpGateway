using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class CreateServiceReportTool
{
    [McpServerTool(Name = "create_service_report"), Description("Create an engagement service report.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        string serviceRequestId,
        string templateId,
        int templateVersion,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(serviceRequestId, nameof(serviceRequestId), out var error) ||
            !EngagementToolInput.Required(templateId, nameof(templateId), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CreateServiceReportAsync(
            new CreateServiceReportRequest
            {
                ServiceRequestId = EngagementToolInput.ToGuid(serviceRequestId),
                TemplateId = EngagementToolInput.ToGuid(templateId),
                TemplateVersion = templateVersion,
            },
            cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapReport(response));
    }
}
