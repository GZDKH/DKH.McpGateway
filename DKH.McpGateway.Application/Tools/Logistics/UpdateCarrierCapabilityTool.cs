using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class UpdateCarrierCapabilityTool
{
    [McpServerTool(Name = "update_carrier_capability"), Description(
        "Update a carrier capability record. Partial update: empty scalars are no-ops. " +
        "Set replaceServiceLevels/replaceSupportedZones/replaceRestrictions=true to swap the repeated set " +
        "(true + empty list = clear). " +
        "Returns the updated record. Returns NotFound on bad ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Carrier capability record ID (required)")] string id,
        [Description("New display name (optional)")] string? displayName = null,
        [Description("Max parcel weight value (decimal-as-string, optional)")] string? maxParcelWeightValue = null,
        [Description("Max parcel weight unit (Kilogram, Gram, Pound)")] string? maxParcelWeightUnit = null,
        [Description("Max longest side cm (decimal-as-string, optional)")] string? maxLongestSideCm = null,
        [Description("Replace service levels flag (true = swap the list)")] bool replaceServiceLevels = false,
        [Description("New service levels as comma-separated (e.g. Standard,Express)")] string? serviceLevels = null,
        [Description("Replace supported zones flag")] bool replaceSupportedZones = false,
        [Description("New supported zone codes as comma-separated")] string? supportedZones = null,
        [Description("Replace restrictions flag")] bool replaceRestrictions = false,
        [Description("New restriction tags as comma-separated")] string? restrictions = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateCarrierCapabilityRequest
        {
            Id = id,
            DisplayName = displayName ?? string.Empty,
            MaxLongestSideCm = maxLongestSideCm ?? string.Empty,
            ReplaceServiceLevels = replaceServiceLevels,
            ReplaceSupportedZones = replaceSupportedZones,
            ReplaceRestrictions = replaceRestrictions,
        };

        if (!string.IsNullOrWhiteSpace(maxParcelWeightValue))
        {
            request.MaxParcelWeight = new WeightModel
            {
                Value = maxParcelWeightValue,
                Unit = LogisticsEnumHelper.ParseWeightUnit(maxParcelWeightUnit),
            };
        }

        if (!string.IsNullOrWhiteSpace(serviceLevels))
        {
            foreach (var level in serviceLevels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.ServiceLevels.Add(LogisticsEnumHelper.ParseServiceLevel(level));
            }
        }

        if (!string.IsNullOrWhiteSpace(supportedZones))
        {
            foreach (var zone in supportedZones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.SupportedZones.Add(zone);
            }
        }

        if (!string.IsNullOrWhiteSpace(restrictions))
        {
            foreach (var tag in restrictions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.Restrictions.Add(tag);
            }
        }

        var capability = await client.UpdateCarrierCapabilityAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCarrierCapabilityTool.MapCarrierCapability(capability), McpJsonDefaults.Options);
    }
}
