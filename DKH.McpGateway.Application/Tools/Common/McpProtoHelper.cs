using Google.Protobuf;

namespace DKH.McpGateway.Application.Tools.Common;

internal static class McpProtoHelper
{
    internal static readonly JsonParser Parser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
    internal static readonly JsonFormatter Formatter = new(JsonFormatter.Settings.Default.WithIndentation());

    internal static string FormatManageResponse(bool success, string action, string code, IEnumerable<string> errors)
    {
        return JsonSerializer.Serialize(new { success, action, code, errors }, McpJsonDefaults.Options);
    }

    internal static string FormatManageResponse(string code, IEnumerable<string> errors)
    {
        var errorList = errors as ICollection<string> ?? [.. errors];
        return JsonSerializer.Serialize(new { success = errorList.Count == 0, code, errors = errorList }, McpJsonDefaults.Options);
    }

    internal static string FormatGetResponse<TData>(bool found, TData? data) where TData : IMessage
    {
        return found && data is not null
            ? Formatter.Format(data)
            : JsonSerializer.Serialize(new { found = false }, McpJsonDefaults.Options);
    }

    internal static string FormatListResponse<TData>(
        IEnumerable<TData> items, int totalCount, int page, int pageSize) where TData : IMessage
    {
        return JsonSerializer.Serialize(new
        {
            items = items.Select(i => JsonSerializer.Deserialize<JsonElement>(Formatter.Format(i))),
            totalCount,
            page,
            pageSize,
        }, McpJsonDefaults.Options);
    }

    internal static string FormatError(string message)
    {
        return JsonSerializer.Serialize(new { success = false, error = message }, McpJsonDefaults.Options);
    }
}
