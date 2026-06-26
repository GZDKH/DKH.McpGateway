using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class CreateSurchargeRuleTool
{
    [McpServerTool(Name = "create_surcharge_rule"), Description(
        "Create a surcharge rule applied on top of base rate card pricing. " +
        "Value is a decimal-as-string: flat amount for Flat mode, fraction 0..5 for Percentage mode. " +
        "Currency is required for Flat mode. Carrier is empty for global scope. " +
        "Returns InvalidArgument on missing/conflicting fields.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SurchargeRuleService.SurchargeRuleServiceClient client,
        [Description("Rule name")] string name,
        [Description("Surcharge kind: Fuel, Zone, Oversize, Scenario")] string kind,
        [Description("Calculation mode: Flat or Percentage")] string mode,
        [Description("Value: flat amount or percentage fraction 0..5 (decimal-as-string)")] string value,
        [Description("Currency code (required for Flat mode, e.g. USD)")] string? currency = null,
        [Description("Carrier code (empty = global scope)")] string? carrier = null,
        [Description("Validity start unix ms")] long validityStartUnixMs = 0,
        [Description("Validity end unix ms (0 = open-ended)")] long validityEndUnixMs = 0,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(name))
        {
            return McpProtoHelper.FormatError("name is required");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return McpProtoHelper.FormatError("value is required");
        }

        var request = new CreateSurchargeRuleRequest
        {
            Name = name,
            Kind = LogisticsEnumHelper.ParseSurchargeKind(kind),
            Mode = LogisticsEnumHelper.ParseSurchargeCalculationMode(mode),
            Value = value,
            Currency = currency ?? string.Empty,
            Carrier = carrier ?? string.Empty,
            Validity = new ValidityWindowModel
            {
                StartUnixMs = validityStartUnixMs,
                EndUnixMs = validityEndUnixMs,
            },
        };

        var rule = await client.CreateSurchargeRuleAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSurchargeRuleTool.MapSurchargeRule(rule), McpJsonDefaults.Options);
    }
}
