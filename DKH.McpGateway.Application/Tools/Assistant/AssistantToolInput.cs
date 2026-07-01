using System.Diagnostics.CodeAnalysis;
using AssistantChannel = DKH.AssistantService.Contracts.Assistant.Models.V1.Channel;

namespace DKH.McpGateway.Application.Tools.Assistant;

internal static class AssistantToolInput
{
    internal static bool Required(string? value, string fieldName, [NotNullWhen(false)] out string? error)
    {
        error = string.IsNullOrWhiteSpace(value) ? $"{fieldName} is required" : null;
        return error is null;
    }

    internal static string RequiredString(string value) => value.Trim();

    internal static void SetOptional(string? value, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            assign(value.Trim());
        }
    }

    internal static AssistantChannel ParseChannel(string? channel)
    {
        var value = string.IsNullOrWhiteSpace(channel) ? "api" : channel.Trim();
        if (Enum.TryParse(
            value,
            ignoreCase: true,
            out AssistantChannel parsed))
        {
            return parsed;
        }

        return Enum.TryParse("Channel" + value, ignoreCase: true, out parsed)
            ? parsed
            : AssistantChannel.Api;
    }
}
