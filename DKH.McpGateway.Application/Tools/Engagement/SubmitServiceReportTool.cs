using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class SubmitServiceReportTool
{
    [McpServerTool(Name = "submit_service_report"), Description("Submit an engagement service report.")]
    public static async Task<string> ExecuteAsync(IApiKeyContext apiKeyContext, EngagementGrpc.EngagementServiceClient client, string id, CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(id, nameof(id), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.SubmitServiceReportAsync(new SubmitServiceReportRequest { Id = EngagementToolInput.ToGuid(id) }, cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapReport(response));
    }
}
