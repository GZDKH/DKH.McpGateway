using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class ActivateSurchargeRuleTool
{
    [McpServerTool(Name = "activate_surcharge_rule"), Description(
        "Activate a surcharge rule, enabling it during rate calculation. " +
        "Returns the updated rule. " +
        "Returns NotFound on bad ID, FailedPrecondition when the rule is already expired.")]
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

        var rule = await client.ActivateSurchargeRuleAsync(
            new ActivateSurchargeRuleRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSurchargeRuleTool.MapSurchargeRule(rule), McpJsonDefaults.Options);
    }
}
