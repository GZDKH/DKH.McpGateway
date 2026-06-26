using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class DeleteSurchargeRuleTool
{
    [McpServerTool(Name = "delete_surcharge_rule"), Description(
        "Delete a surcharge rule by ID. " +
        "Returns NotFound when the ID does not exist.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SurchargeRuleService.SurchargeRuleServiceClient client,
        [Description("Surcharge rule ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.DeleteSurchargeRuleAsync(
            new DeleteSurchargeRuleRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { deleted = true, id }, McpJsonDefaults.Options);
    }
}
