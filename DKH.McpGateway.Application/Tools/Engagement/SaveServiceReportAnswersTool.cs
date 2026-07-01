using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class SaveServiceReportAnswersTool
{
    [McpServerTool(Name = "save_service_report_answers"), Description("Save engagement service report answers.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        string id,
        [Description("Answers JSON array")] string? answersJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(id, nameof(id), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var answers = EngagementToolInput.ParseAnswers(answersJson, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var request = new SaveServiceReportAnswersRequest { Id = EngagementToolInput.ToGuid(id) };
        request.Answers.Add(answers);
        var response = await client.SaveServiceReportAnswersAsync(request, cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapReport(response));
    }
}
