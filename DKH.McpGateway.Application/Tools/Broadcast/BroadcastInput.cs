using System.Globalization;
using DKH.BroadcastService.Contracts.Broadcast.Models.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Broadcast;

internal static class BroadcastInput
{
    internal static BroadcastStatus ParseStatus(string? status)
    {
        return System.Enum.TryParse<BroadcastStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : BroadcastStatus.Unspecified;
    }

    internal static RecurrenceType ParseRecurrence(string? recurrence)
    {
        return System.Enum.TryParse<RecurrenceType>(recurrence ?? "None", ignoreCase: true, out var parsed)
            ? parsed
            : RecurrenceType.None;
    }

    internal static bool TryParseTimestamp(string? value, out Timestamp? timestamp)
    {
        timestamp = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return false;
        }

        timestamp = Timestamp.FromDateTimeOffset(parsed.ToUniversalTime());
        return true;
    }
}
