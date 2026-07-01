using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DKH.McpGateway.Application.Tools.Staff;

internal static class StaffToolInput
{
    internal static GuidValue ToGuid(string value) => new(value.Trim());

    internal static GuidValue? ToGuidOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new GuidValue(value.Trim());

    internal static PaginationRequest Page(int page, int pageSize)
        => new() { Page = Math.Max(page, 1), PageSize = Math.Clamp(pageSize, 1, 100) };

    internal static string OptionalString(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    internal static bool Required(string? value, string fieldName, [NotNullWhen(false)] out string? error)
    {
        error = string.IsNullOrWhiteSpace(value) ? $"{fieldName} is required" : null;
        return error is null;
    }

    internal static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out parsed))
        {
            return true;
        }

        var normalized = NormalizeEnumToken(value);
        foreach (var enumName in Enum.GetNames<TEnum>())
        {
            var normalizedEnumName = NormalizeEnumToken(enumName);
            if (string.Equals(normalizedEnumName, normalized, StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(normalizedEnumName, StringComparison.OrdinalIgnoreCase))
            {
                parsed = Enum.Parse<TEnum>(enumName);
                return true;
            }
        }

        return false;
    }

    internal static TEnum ParseOptionalEnum<TEnum>(string? value, out string? error)
        where TEnum : struct, Enum
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

    internal static TEnum ParseRequiredEnum<TEnum>(string? value, string fieldName, out string? error)
        where TEnum : struct, Enum
    {
        error = null;
        if (!TryParseEnum<TEnum>(value, out var parsed) || EqualityComparer<TEnum>.Default.Equals(parsed, default))
        {
            error = $"{fieldName} is invalid";
            return default;
        }

        return parsed;
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
}
