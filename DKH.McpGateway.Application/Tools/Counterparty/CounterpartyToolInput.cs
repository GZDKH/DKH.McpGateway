using System.Globalization;
using System.Text;
using DKH.CounterpartyService.Contracts.Counterparty.Models.v1;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Counterparty;

internal static class CounterpartyToolInput
{
    internal static GuidValue ToGuid(string value) => new(value.Trim());

    internal static GuidValue? ToGuidOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new GuidValue(value.Trim());

    internal static PaginationRequest Page(int page, int pageSize)
        => new() { Page = Math.Max(page, 1), PageSize = Math.Clamp(pageSize, 1, 100) };

    internal static string? OptionalString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static Timestamp? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Timestamp.FromDateTime(DateTime.Parse(value, CultureInfo.InvariantCulture).ToUniversalTime());
    }

    internal static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, System.Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (System.Enum.TryParse(value.Trim(), ignoreCase: true, out parsed))
        {
            return true;
        }

        var normalized = NormalizeEnumToken(value);
        foreach (var enumName in System.Enum.GetNames<TEnum>())
        {
            var normalizedEnumName = NormalizeEnumToken(enumName);
            if (string.Equals(normalizedEnumName, normalized, StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(normalizedEnumName, StringComparison.OrdinalIgnoreCase))
            {
                parsed = System.Enum.Parse<TEnum>(enumName);
                return true;
            }
        }

        return false;
    }

    internal static TEnum ParseOptionalEnum<TEnum>(string? value, out string? error)
        where TEnum : struct, System.Enum
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        if (TryParseEnum<TEnum>(value, out var parsed))
        {
            return parsed;
        }

        error = $"{typeof(TEnum).Name} value is invalid";
        return default;
    }

    internal static LocalizedText? ParseLocalizedText(string? input, string fieldName, bool required, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            if (required)
            {
                error = $"{fieldName} is required";
            }

            return null;
        }

        var text = new LocalizedText();
        var trimmed = input.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(trimmed);
                if (values is not null)
                {
                    foreach (var (locale, value) in values.Where(v => !string.IsNullOrWhiteSpace(v.Key)))
                    {
                        text.Values[locale] = value;
                    }
                }
            }
            catch (JsonException)
            {
                error = $"{fieldName} must be plain text or a JSON object of locale-to-text values";
            }
        }
        else
        {
            text.Values["default"] = trimmed;
        }

        if (required && text.Values.Count == 0 && error is null)
        {
            error = $"{fieldName} is required";
        }

        return text;
    }

    internal static string[] ParseStringList(string? input, string fieldName, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var trimmed = input.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<string[]>(trimmed)?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? [];
            }
            catch (JsonException)
            {
                error = $"{fieldName} must be a JSON string array";
                return [];
            }
        }

        return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    internal static GuidValue[] ParseGuidList(string? input, string fieldName, out string? error)
    {
        var values = ParseStringList(input, fieldName, out error);
        return error is null ? [.. values.Select(ToGuid)] : [];
    }

    internal static List<TEnum> ParseEnumList<TEnum>(string? input, string fieldName, out string? error)
        where TEnum : struct, System.Enum
    {
        var values = ParseStringList(input, fieldName, out error);
        if (error is not null)
        {
            return [];
        }

        var parsed = new List<TEnum>();
        foreach (var value in values)
        {
            if (!TryParseEnum<TEnum>(value, out var enumValue))
            {
                error = $"{fieldName} contains invalid {typeof(TEnum).Name}: {value}";
                return [];
            }

            parsed.Add(enumValue);
        }

        return parsed;
    }

    internal static CounterpartyAddress? ParseAddress(string? input, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(input);
            var root = doc.RootElement;
            var address = new CounterpartyAddress
            {
                City = GetString(root, "city"),
                Region = GetString(root, "region"),
                Country = GetString(root, "country"),
                PostalCode = GetString(root, "postalCode") ?? GetString(root, "postal_code"),
            };

            if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
            {
                address.Lines.Add(lines.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            return address;
        }
        catch (JsonException)
        {
            error = "addressJson must be a JSON object";
            return null;
        }
    }

    internal static CounterpartyCapabilitiesModel? ParseCapabilities(string? input, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(input);
            var root = doc.RootElement;
            var model = new CounterpartyCapabilitiesModel
            {
                MaxWeightKg = GetString(root, "maxWeightKg") ?? GetString(root, "max_weight_kg"),
                SupportsHazmat = GetBool(root, "supportsHazmat") ?? GetBool(root, "supports_hazmat") ?? false,
                SupportsColdChain = GetBool(root, "supportsColdChain") ?? GetBool(root, "supports_cold_chain") ?? false,
                SupportsCod = GetBool(root, "supportsCod") ?? GetBool(root, "supports_cod") ?? false,
                SupportsInsurance = GetBool(root, "supportsInsurance") ?? GetBool(root, "supports_insurance") ?? false,
                SupportsTracking = GetBool(root, "supportsTracking") ?? GetBool(root, "supports_tracking") ?? false,
            };

            AddTransportModes(root, "transportModes", model.TransportModes, ref error);
            AddTransportModes(root, "transport_modes", model.TransportModes, ref error);
            if (error is not null)
            {
                return null;
            }

            AddGuidArray(root, "serviceZoneIds", model.ServiceZoneIds);
            AddGuidArray(root, "service_zone_ids", model.ServiceZoneIds);
            AddStringArray(root, "customCapabilities", model.CustomCapabilities);
            AddStringArray(root, "custom_capabilities", model.CustomCapabilities);

            if (root.TryGetProperty("maxDimensions", out var dimensions) || root.TryGetProperty("max_dimensions", out dimensions))
            {
                model.MaxDimensions = new CounterpartyDimensionsModel
                {
                    LengthCm = GetString(dimensions, "lengthCm") ?? GetString(dimensions, "length_cm") ?? string.Empty,
                    WidthCm = GetString(dimensions, "widthCm") ?? GetString(dimensions, "width_cm") ?? string.Empty,
                    HeightCm = GetString(dimensions, "heightCm") ?? GetString(dimensions, "height_cm") ?? string.Empty,
                };
            }

            return model;
        }
        catch (JsonException)
        {
            error = "capabilitiesJson must be a JSON object";
            return null;
        }
    }

    private static string NormalizeEnumToken(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    private static bool? GetBool(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static void AddGuidArray(JsonElement root, string propertyName, RepeatedField<GuidValue> target)
    {
        if (root.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in array.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                target.Add(ToGuid(value!));
            }
        }
    }

    private static void AddStringArray(JsonElement root, string propertyName, RepeatedField<string> target)
    {
        if (root.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in array.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                target.Add(value!);
            }
        }
    }

    private static void AddTransportModes(JsonElement root, string propertyName, RepeatedField<TransportMode> target, ref string? error)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var value in array.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            if (!TryParseEnum<TransportMode>(value, out var parsed))
            {
                error = $"{propertyName} contains invalid {nameof(TransportMode)}: {value}";
                return;
            }

            target.Add(parsed);
        }
    }
}
