using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

internal static class TelegramClientToolInput
{
    internal static GuidValue ToGuid(string value) => new(value.Trim());

    internal static bool Required(string? value, string fieldName, [NotNullWhen(false)] out string? error)
    {
        error = string.IsNullOrWhiteSpace(value) ? $"{fieldName} is required" : null;
        return error is null;
    }

    internal static string RequiredString(string value) => value.Trim();

    internal static string OptionalString(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    internal static int Page(int page) => Math.Max(page, 1);

    internal static int PageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);

    internal static Timestamp? ParseTimestamp(string? value, string fieldName, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            error = $"{fieldName} must be an ISO-8601 timestamp";
            return null;
        }

        return Timestamp.FromDateTimeOffset(parsed.ToUniversalTime());
    }
}
