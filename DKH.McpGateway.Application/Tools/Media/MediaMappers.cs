using DKH.MediaService.Contracts.Api.V1;
using DKH.MediaService.Contracts.Entities.V1;

namespace DKH.McpGateway.Application.Tools.Media;

/// <summary>
/// Shared response mappers for MediaService MCP tools.
/// All id/string fields on these contracts are plain proto <c>string</c> (no wrapper types).
/// </summary>
internal static class MediaMappers
{
    // AttachmentModel — all string/int/long are PLAIN (no .Value). attached_by_id OMITTED (actor id).
    internal static object MapAttachment(AttachmentModel a) => new
    {
        attachmentId = a.AttachmentId,
        assetId = a.AssetId,
        scope = a.Scope,
        scopeKey = a.ScopeKey,
        role = a.Role,
        sortOrder = a.SortOrder,
        container = a.Container,
        key = a.Key,
        contentType = a.ContentType,
        lengthBytes = a.LengthBytes,
        altText = a.AltText,
        caption = a.Caption,
        // attachedById omitted — internal actor GUID (PII-adjacent)
        attachedAtUtc = a.AttachedAtUtc?.ToDateTimeOffset().ToString("O"),
    };

    // UploadSessionModel — status is enum (.ToString()); requested_by_id OMITTED (actor id).
    internal static object MapUploadSession(UploadSessionModel s) => new
    {
        id = s.Id,
        scope = s.Scope,
        scopeKey = s.ScopeKey,
        subKind = s.SubKind,
        status = s.Status.ToString(),
        // requestedById omitted — internal actor GUID
        requestedAtUtc = s.RequestedAtUtc?.ToDateTimeOffset().ToString("O"),
        expiresAtUtc = s.ExpiresAtUtc?.ToDateTimeOffset().ToString("O"),
        uploadUrl = s.UploadUrl,
    };

    // MediaRefModel — all plain.
    internal static object MapMediaRef(MediaRefModel m) => new
    {
        assetId = m.AssetId,
        container = m.Container,
        key = m.Key,
        contentType = m.ContentType,
        lengthBytes = m.LengthBytes,
    };
}
