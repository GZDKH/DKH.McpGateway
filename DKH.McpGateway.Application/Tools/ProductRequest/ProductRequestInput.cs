using DKH.ProductRequestService.Contracts.ProductRequest.Models.v1;

namespace DKH.McpGateway.Application.Tools.ProductRequest;

internal static class ProductRequestInput
{
    internal static ProductRequestStatus ParseStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant().Replace("_", "") switch
        {
            "new" => ProductRequestStatus.New,
            "inreview" or "review" => ProductRequestStatus.InReview,
            "found" => ProductRequestStatus.Found,
            "notfound" => ProductRequestStatus.NotFound,
            "cancelled" or "canceled" => ProductRequestStatus.Cancelled,
            _ => ProductRequestStatus.Unspecified,
        };
    }

    internal static SoftDeleteFilter ParseSoftDeleteFilter(string? filter)
    {
        return filter?.Trim().ToLowerInvariant() switch
        {
            "deleted" => SoftDeleteFilter.InactiveOnly,
            "all" => SoftDeleteFilter.All,
            _ => SoftDeleteFilter.ActiveOnly,
        };
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
                return JsonSerializer.Deserialize<string[]>(trimmed, McpJsonDefaults.Options)?
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .ToArray() ?? [];
            }
            catch (JsonException)
            {
                error = $"{fieldName} JSON is invalid";
                return [];
            }
        }

        return
        [
            .. trimmed
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static value => !string.IsNullOrWhiteSpace(value)),
        ];
    }
}
