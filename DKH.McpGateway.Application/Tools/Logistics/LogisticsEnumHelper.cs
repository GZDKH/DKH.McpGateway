using DKH.LogisticsService.Contracts.Models.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

/// <summary>
/// Shared enum parse helpers for LogisticsService tools.
/// Generated enum member names are simple (Economy, Standard, Flat …) — no EnumName prefix.
/// </summary>
internal static class LogisticsEnumHelper
{
    internal static ServiceLevel ParseServiceLevel(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "economy" => ServiceLevel.Economy,
        "standard" => ServiceLevel.Standard,
        "express" => ServiceLevel.Express,
        "same_day" or "sameday" => ServiceLevel.SameDay,
        _ => ServiceLevel.Unspecified,
    };

    internal static SurchargeKind ParseSurchargeKind(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "fuel" => SurchargeKind.Fuel,
        "zone" => SurchargeKind.Zone,
        "oversize" => SurchargeKind.Oversize,
        "scenario" => SurchargeKind.Scenario,
        _ => SurchargeKind.Unspecified,
    };

    internal static SurchargeCalculationMode ParseSurchargeCalculationMode(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "flat" => SurchargeCalculationMode.Flat,
        "percentage" => SurchargeCalculationMode.Percentage,
        _ => SurchargeCalculationMode.Unspecified,
    };

    internal static TariffPricingMode ParseTariffPricingMode(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "flat" => TariffPricingMode.Flat,
        "perkilogram" or "per_kilogram" => TariffPricingMode.PerKilogram,
        _ => TariffPricingMode.Unspecified,
    };

    internal static WeightUnit ParseWeightUnit(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "kilogram" or "kg" => WeightUnit.Kilogram,
        "gram" or "g" => WeightUnit.Gram,
        "pound" or "lb" => WeightUnit.Pound,
        _ => WeightUnit.Unspecified,
    };

    internal static LengthUnit ParseLengthUnit(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "centimeter" or "cm" => LengthUnit.Centimeter,
        "millimeter" or "mm" => LengthUnit.Millimeter,
        "inch" or "in" => LengthUnit.Inch,
        _ => LengthUnit.Unspecified,
    };
}
