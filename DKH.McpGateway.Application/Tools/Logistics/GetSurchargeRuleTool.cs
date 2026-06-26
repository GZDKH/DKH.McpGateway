using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class GetSurchargeRuleTool
{
    [McpServerTool(Name = "get_surcharge_rule"), Description(
        "Get a single surcharge rule by its ID. " +
        "Returns kind, mode, value, carrier scope, validity and active status. " +
        "Returns NotFound when the ID does not exist.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SurchargeRuleService.SurchargeRuleServiceClient client,
        [Description("Surcharge rule ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var rule = await client.GetSurchargeRuleAsync(
            new GetSurchargeRuleRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapSurchargeRule(rule), McpJsonDefaults.Options);
    }

    internal static object MapSurchargeRule(SurchargeRuleModel r) => new
    {
        id = r.Id,
        name = r.Name,
        kind = r.Kind.ToString(),
        mode = r.Mode.ToString(),
        value = r.Value,
        currency = r.Currency,
        carrier = r.Carrier,
        isActive = r.IsActive,
        validity = r.Validity is null ? null : (object)new
        {
            startUnixMs = r.Validity.StartUnixMs,
            endUnixMs = r.Validity.EndUnixMs,
        },
    };
}
