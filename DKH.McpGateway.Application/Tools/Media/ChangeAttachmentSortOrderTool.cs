using DKH.MediaService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Media;

[McpServerToolType]
public static class ChangeAttachmentSortOrderTool
{
    [McpServerTool(Name = "media_change_attachment_sort_order"), Description(
        "Reorder attachments within a scope + scope key + role. " +
        "items should be a JSON array of {\"attachmentId\":\"...\",\"sortOrder\":N} objects.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AttachmentService.AttachmentServiceClient client,
        [Description("Attachment scope (e.g. product, category)")] string scope,
        [Description("Scope key (entity ID the attachments belong to)")] string scopeKey,
        [Description("Attachment role")] string role,
        [Description("Sort order items as JSON: [{\"attachmentId\":\"...\",\"sortOrder\":0}]")] string items,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(scope))
        {
            return McpProtoHelper.FormatError("scope is required");
        }

        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            return McpProtoHelper.FormatError("scopeKey is required");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return McpProtoHelper.FormatError("role is required");
        }

        if (string.IsNullOrWhiteSpace(items))
        {
            return McpProtoHelper.FormatError("items is required");
        }

        List<SortOrderItemInput>? parsedItems;
        try
        {
            parsedItems = JsonSerializer.Deserialize<List<SortOrderItemInput>>(items, McpJsonDefaults.Options);
        }
        catch (JsonException)
        {
            return McpProtoHelper.FormatError("items JSON is invalid");
        }

        if (parsedItems is null || parsedItems.Count == 0)
        {
            return McpProtoHelper.FormatError("items must contain at least one item");
        }

        var request = new ChangeSortOrderRequest { Scope = scope, ScopeKey = scopeKey, Role = role };

        foreach (var item in parsedItems)
        {
            if (string.IsNullOrWhiteSpace(item.AttachmentId))
            {
                return McpProtoHelper.FormatError("items[].attachmentId is required");
            }

            request.Items.Add(new SortOrderItem
            {
                AttachmentId = item.AttachmentId.Trim(),
                SortOrder = item.SortOrder,
            });
        }

        await client.ChangeSortOrderAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { ok = true }, McpJsonDefaults.Options);
    }

    private sealed class SortOrderItemInput
    {
        public string? AttachmentId { get; set; }

        public int SortOrder { get; set; }
    }
}
