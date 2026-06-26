using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class DeactivateSurchargeRuleTool
{
    [McpServerTool(Name = "deactivate_surcharge_rule"), Description(
        "Deactivate a surcharge rule, disabling it during rate calculation. " +
        "Returns the updated rule. " +
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

        var rule = await client.DeactivateSurchargeRuleAsync(
            new DeactivateSurchargeRuleRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSurchargeRuleTool.MapSurchargeRule(rule), McpJsonDefaults.Options);
    }
}
