using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class UpdateSurchargeRuleTool
{
    [McpServerTool(Name = "update_surcharge_rule"), Description(
        "Update a surcharge rule. Partial update: empty strings are no-ops. " +
        "Use flatAmount+flatCurrency for Flat-mode rules, percentageFraction for Percentage-mode. " +
        "Set clearCarrier=true to remove carrier scope (make global). " +
        "Returns NotFound on bad ID, FailedPrecondition when editing requires the rule to be inactive.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SurchargeRuleService.SurchargeRuleServiceClient client,
        [Description("Surcharge rule ID (required)")] string id,
        [Description("New name (optional)")] string? name = null,
        [Description("Flat amount value (decimal-as-string, for Flat mode)")] string? flatAmount = null,
        [Description("Flat amount currency code")] string? flatCurrency = null,
        [Description("Percentage fraction 0..5 (decimal-as-string, for Percentage mode)")] string? percentageFraction = null,
        [Description("New carrier code (optional)")] string? carrier = null,
        [Description("Set true to remove carrier scope (make global)")] bool clearCarrier = false,
        [Description("Validity start unix ms (optional)")] long? validityStartUnixMs = null,
        [Description("Validity end unix ms (optional, 0 = open-ended)")] long? validityEndUnixMs = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateSurchargeRuleRequest
        {
            Id = id,
            Name = name ?? string.Empty,
            FlatAmount = flatAmount ?? string.Empty,
            FlatCurrency = flatCurrency ?? string.Empty,
            PercentageFraction = percentageFraction ?? string.Empty,
            Carrier = carrier ?? string.Empty,
            ClearCarrier = clearCarrier,
        };

        if (validityStartUnixMs.HasValue || validityEndUnixMs.HasValue)
        {
            request.Validity = new ValidityWindowModel
            {
                StartUnixMs = validityStartUnixMs ?? 0,
                EndUnixMs = validityEndUnixMs ?? 0,
            };
        }

        var rule = await client.UpdateSurchargeRuleAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSurchargeRuleTool.MapSurchargeRule(rule), McpJsonDefaults.Options);
    }
}
