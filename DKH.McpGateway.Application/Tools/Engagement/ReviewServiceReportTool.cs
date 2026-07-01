using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class ReviewServiceReportTool
{
    [McpServerTool(Name = "review_service_report"), Description("Accept or reject an engagement service report.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        string id,
        bool accept,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(id, nameof(id), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ReviewServiceReportAsync(
            new ReviewServiceReportRequest
            {
                Id = EngagementToolInput.ToGuid(id),
                Accept = accept,
                Reason = EngagementToolInput.OptionalString(reason),
            },
            cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapReport(response));
    }
}
