using System.Globalization;
using DKH.CustomsService.Contracts.DutyRule.Models.V2;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Customs;

internal static class CustomsToolInput
{
    internal static readonly JsonSerializerOptions JsonOptions = new(McpJsonDefaults.Options)
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static Timestamp? ParseTimestamp(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));

    internal static TEnum ParseEnumOrDefault<TEnum>(string? value)
        where TEnum : struct, System.Enum
        => !string.IsNullOrWhiteSpace(value) && System.Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : default;

    internal static string? PopulateDutyLines(RepeatedField<DutyLine> destination, string? linesJson)
    {
        if (string.IsNullOrWhiteSpace(linesJson))
        {
            return "linesJson is required";
        }

        try
        {
            var lines = JsonSerializer.Deserialize<List<DutyLineInput>>(linesJson, JsonOptions);
            if (lines is null || lines.Count == 0)
            {
                return "linesJson must contain at least one line";
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.HsCode))
                {
                    return "line.hsCode is required";
                }

                if (string.IsNullOrWhiteSpace(line.Quantity))
                {
                    return "line.quantity is required";
                }

                if (string.IsNullOrWhiteSpace(line.DeclaredValue))
                {
                    return "line.declaredValue is required";
                }

                if (string.IsNullOrWhiteSpace(line.ValueCurrency))
                {
                    return "line.valueCurrency is required";
                }

                destination.Add(new DutyLine
                {
                    HsCode = line.HsCode,
                    SystemCode = line.SystemCode ?? string.Empty,
                    Origin = line.Origin ?? string.Empty,
                    Quantity = line.Quantity,
                    DeclaredValue = line.DeclaredValue,
                    ValueCurrency = line.ValueCurrency,
                });
            }
        }
        catch (JsonException)
        {
            return "linesJson is invalid JSON";
        }

        return null;
    }

    private sealed record DutyLineInput(
        string HsCode,
        string? SystemCode,
        string? Origin,
        string Quantity,
        string DeclaredValue,
        string ValueCurrency);
}
