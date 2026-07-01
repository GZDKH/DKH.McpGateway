using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Models.v1;

namespace DKH.McpGateway.Application.Tools.Counterparty;

[McpServerToolType]
public static class GetCounterpartyTool
{
    [McpServerTool(Name = "get_counterparty"), Description("Get a counterparty by ID with PII fields omitted from the response.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(counterpartyId))
        {
            return McpProtoHelper.FormatError("counterpartyId is required");
        }

        var response = await client.GetCounterpartyAsync(
            new GetCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(counterpartyId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapCounterparty(response));
    }
}

[McpServerToolType]
public static class ListCounterpartiesTool
{
    [McpServerTool(Name = "list_counterparties"), Description(
        "List counterparties with optional filters. PII fields are omitted from every item.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Roles filter as JSON array or CSV (optional)")] string? roles = null,
        [Description("CounterpartyKind filter (optional)")] string? kind = null,
        [Description("CounterpartyStatus filter (optional)")] string? status = null,
        [Description("Search term (optional; response still omits PII)")] string? search = null,
        [Description("CounterpartyTier filter (optional)")] string? tier = null,
        [Description("PartnerRelationshipStatus filter (optional)")] string? partnerStatus = null,
        [Description("Minimum days since last order; 0 disables filter")] int minDaysSinceLastOrder = 0,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListCounterpartiesRequest
        {
            Pagination = CounterpartyToolInput.Page(page, pageSize),
            Search = CounterpartyToolInput.OptionalString(search),
            MinDaysSinceLastOrder = Math.Max(minDaysSinceLastOrder, 0),
        };

        request.Roles.Add(CounterpartyToolInput.ParseEnumList<CounterpartyRole>(roles, nameof(roles), out var error));
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        request.Kind = CounterpartyToolInput.ParseOptionalEnum<CounterpartyKind>(kind, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        request.Status = CounterpartyToolInput.ParseOptionalEnum<CounterpartyStatus>(status, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        request.Tier = CounterpartyToolInput.ParseOptionalEnum<CounterpartyTier>(tier, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        request.PartnerStatus = CounterpartyToolInput.ParseOptionalEnum<PartnerRelationshipStatus>(partnerStatus, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListCounterpartiesAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new
        {
            items = response.Items.Select(CounterpartyMapper.MapCounterparty),
            page = CounterpartyMapper.MapPage(response.Metadata, request.Pagination.Page, request.Pagination.PageSize),
        });
    }
}

[McpServerToolType]
public static class CreateCounterpartyTool
{
    [McpServerTool(Name = "create_counterparty"), Description(
        "Create a counterparty. Accepts PII inputs such as email/phone/address/legal identifiers, but the response always omits them.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("CounterpartyKind, e.g. Company")] string kind,
        [Description("Display name text or JSON locale map")] string displayName,
        [Description("Roles as JSON array or CSV (optional)")] string? roles = null,
        [Description("VerificationLevel (optional)")] string? verificationLevel = null,
        [Description("DataSource (optional)")] string? dataSource = null,
        [Description("VisibilityMode (optional)")] string? visibilityMode = null,
        [Description("Storefront scope IDs as JSON array or CSV (optional)")] string? storefrontScopeIds = null,
        [Description("Tags as JSON array or CSV (optional)")] string? tags = null,
        [Description("Description text or JSON locale map (optional)")] string? description = null,
        [Description("Parent counterparty ID (optional)")] string? parentCounterpartyId = null,
        [Description("Legal name PII input (optional; not echoed)")] string? legalName = null,
        [Description("Tax ID PII input (optional; not echoed)")] string? taxId = null,
        [Description("Registration number PII input (optional; not echoed)")] string? registrationNumber = null,
        [Description("Email PII input (optional; not echoed)")] string? email = null,
        [Description("Phone PII input (optional; not echoed)")] string? phone = null,
        [Description("Address JSON PII input (optional; not echoed)")] string? addressJson = null,
        [Description("Avatar attachment ID (optional)")] string? avatarAttachmentId = null,
        [Description("Document attachment IDs as JSON array or CSV (optional)")] string? documentAttachmentIds = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var request = CounterpartyCrudToolRequests.Create(
            kind, displayName, roles, verificationLevel, dataSource, visibilityMode, storefrontScopeIds, tags,
            description, parentCounterpartyId, legalName, taxId, registrationNumber, email, phone, addressJson,
            avatarAttachmentId, documentAttachmentIds, out var error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CreateCounterpartyAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapCounterparty(response));
    }
}

[McpServerToolType]
public static class UpdateCounterpartyTool
{
    [McpServerTool(Name = "update_counterparty"), Description(
        "Update a counterparty. Accepts PII inputs, but the response always omits legal/tax/contact/address fields.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("Display name text or JSON locale map (optional)")] string? displayName = null,
        [Description("Roles as JSON array or CSV (optional)")] string? roles = null,
        [Description("CounterpartyStatus (optional)")] string? status = null,
        [Description("VerificationLevel (optional)")] string? verificationLevel = null,
        [Description("VisibilityMode (optional)")] string? visibilityMode = null,
        [Description("Storefront scope IDs as JSON array or CSV (optional)")] string? storefrontScopeIds = null,
        [Description("Tags as JSON array or CSV (optional)")] string? tags = null,
        [Description("Description text or JSON locale map (optional)")] string? description = null,
        [Description("Parent counterparty ID (optional)")] string? parentCounterpartyId = null,
        [Description("Legal name PII input (optional; not echoed)")] string? legalName = null,
        [Description("Tax ID PII input (optional; not echoed)")] string? taxId = null,
        [Description("Registration number PII input (optional; not echoed)")] string? registrationNumber = null,
        [Description("Email PII input (optional; not echoed)")] string? email = null,
        [Description("Phone PII input (optional; not echoed)")] string? phone = null,
        [Description("Address JSON PII input (optional; not echoed)")] string? addressJson = null,
        [Description("Avatar attachment ID (optional)")] string? avatarAttachmentId = null,
        [Description("Clear avatar attachment flag")] bool clearAvatarAttachment = false,
        [Description("Document attachment IDs as JSON array or CSV (optional)")] string? documentAttachmentIds = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var request = CounterpartyCrudToolRequests.Update(
            counterpartyId, displayName, roles, status, verificationLevel, visibilityMode, storefrontScopeIds, tags,
            description, parentCounterpartyId, legalName, taxId, registrationNumber, email, phone, addressJson,
            avatarAttachmentId, clearAvatarAttachment, documentAttachmentIds, out var error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.UpdateCounterpartyAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapCounterparty(response));
    }
}

[McpServerToolType]
public static class ArchiveCounterpartyTool
{
    [McpServerTool(Name = "archive_counterparty"), Description("Archive a counterparty by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("Archive reason (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        await client.ArchiveCounterpartyAsync(
            new ArchiveCounterpartyRequest
            {
                Id = CounterpartyToolInput.ToGuid(counterpartyId),
                Reason = CounterpartyToolInput.OptionalString(reason),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { archived = true, counterpartyId });
    }
}

[McpServerToolType]
public static class BatchGetCounterpartyBasicsTool
{
    [McpServerTool(Name = "batch_get_counterparty_basics"), Description("Fetch lightweight non-PII counterparty basics for up to 1000 IDs.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty IDs as JSON array or CSV")] string ids,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new BatchGetCounterpartyBasicsRequest();
        request.Ids.Add(CounterpartyToolInput.ParseGuidList(ids, nameof(ids), out var error));
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.BatchGetCounterpartyBasicsAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new { basics = response.Basics.Select(CounterpartyMapper.MapBasics) });
    }
}

[McpServerToolType]
public static class SetCounterpartyCapabilitiesTool
{
    [McpServerTool(Name = "set_counterparty_capabilities"), Description(
        "Replace or clear logistics/service capabilities for a counterparty. Empty capabilitiesJson clears them.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("Capabilities JSON object; omit/empty to clear")] string? capabilitiesJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var request = new SetCounterpartyCapabilitiesRequest
        {
            Id = CounterpartyToolInput.ToGuid(counterpartyId),
            Capabilities = CounterpartyToolInput.ParseCapabilities(capabilitiesJson, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.SetCounterpartyCapabilitiesAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapCounterparty(response));
    }
}

[McpServerToolType]
public static class AttachCounterpartyMediaTool
{
    [McpServerTool(Name = "attach_counterparty_media"), Description("Attach a media reference to a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("CounterpartyMediaKind")] string kind,
        [Description("Storage file reference")] string storageFileRef,
        [Description("Display order")] int displayOrder = 0,
        [Description("Whether this media is primary")] bool isPrimary = false,
        [Description("Alt text as text or JSON locale map (optional)")] string? altText = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!CounterpartyToolInput.TryParseEnum<CounterpartyMediaKind>(kind, out var mediaKind))
        {
            return McpProtoHelper.FormatError("kind is invalid");
        }

        var request = new AttachCounterpartyMediaRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Kind = mediaKind,
            StorageFileRef = storageFileRef,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary,
            AltText = CounterpartyToolInput.ParseLocalizedText(altText, nameof(altText), required: false, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.AttachCounterpartyMediaAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapMedia(response));
    }
}

[McpServerToolType]
public static class DetachCounterpartyMediaTool
{
    [McpServerTool(Name = "detach_counterparty_media"), Description("Detach a media reference from a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Media row ID")] string mediaId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        await client.DetachCounterpartyMediaAsync(
            new DetachCounterpartyMediaRequest { MediaId = CounterpartyToolInput.ToGuid(mediaId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { detached = true, mediaId });
    }
}

[McpServerToolType]
public static class SetPrimaryCounterpartyMediaTool
{
    [McpServerTool(Name = "set_primary_counterparty_media"), Description("Promote a media row to primary.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Media row ID")] string mediaId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.SetPrimaryCounterpartyMediaAsync(
            new SetPrimaryCounterpartyMediaRequest { MediaId = CounterpartyToolInput.ToGuid(mediaId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapMedia(response));
    }
}

[McpServerToolType]
public static class ListCounterpartyMediaTool
{
    [McpServerTool(Name = "list_counterparty_media"), Description("List media rows for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("CounterpartyMediaKind filter (optional)")] string? kind = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListCounterpartyMediaRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Kind = CounterpartyToolInput.ParseOptionalEnum<CounterpartyMediaKind>(kind, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListCounterpartyMediaAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new { items = response.Items.Select(CounterpartyMapper.MapMedia) });
    }
}

[McpServerToolType]
public static class AttachCounterpartyDocumentTool
{
    [McpServerTool(Name = "attach_counterparty_document"), Description("Attach a verification/compliance document to a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("CounterpartyDocumentKind")] string kind,
        [Description("Storage file reference")] string storageFileRef,
        [Description("Document number")] string number,
        [Description("Issued-at timestamp (ISO-8601, optional)")] string? issuedAt = null,
        [Description("Valid-until timestamp (ISO-8601, optional)")] string? validUntil = null,
        [Description("Issuing authority (optional)")] string? issuingAuthority = null,
        [Description("Notes text or JSON locale map (optional)")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!CounterpartyToolInput.TryParseEnum<CounterpartyDocumentKind>(kind, out var documentKind))
        {
            return McpProtoHelper.FormatError("kind is invalid");
        }

        var request = new AttachCounterpartyDocumentRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Kind = documentKind,
            StorageFileRef = storageFileRef,
            Number = number,
            IssuedAt = CounterpartyToolInput.ParseTimestamp(issuedAt),
            ValidUntil = CounterpartyToolInput.ParseTimestamp(validUntil),
            IssuingAuthority = CounterpartyToolInput.OptionalString(issuingAuthority),
            Notes = CounterpartyToolInput.ParseLocalizedText(notes, nameof(notes), required: false, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.AttachCounterpartyDocumentAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapDocument(response));
    }
}

[McpServerToolType]
public static class DetachCounterpartyDocumentTool
{
    [McpServerTool(Name = "detach_counterparty_document"), Description("Detach a document row from a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Document row ID")] string documentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        await client.DetachCounterpartyDocumentAsync(
            new DetachCounterpartyDocumentRequest { DocumentId = CounterpartyToolInput.ToGuid(documentId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { detached = true, documentId });
    }
}

[McpServerToolType]
public static class VerifyCounterpartyDocumentTool
{
    [McpServerTool(Name = "verify_counterparty_document"), Description("Mark a counterparty document as verified.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Document row ID")] string documentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.VerifyCounterpartyDocumentAsync(
            new VerifyCounterpartyDocumentRequest { DocumentId = CounterpartyToolInput.ToGuid(documentId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapDocument(response));
    }
}

[McpServerToolType]
public static class RejectCounterpartyDocumentTool
{
    [McpServerTool(Name = "reject_counterparty_document"), Description("Reject a counterparty document with an optional reason.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Document row ID")] string documentId,
        [Description("Rejection reason (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.RejectCounterpartyDocumentAsync(
            new RejectCounterpartyDocumentRequest
            {
                DocumentId = CounterpartyToolInput.ToGuid(documentId),
                Reason = CounterpartyToolInput.OptionalString(reason),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapDocument(response));
    }
}

[McpServerToolType]
public static class ListCounterpartyDocumentsTool
{
    [McpServerTool(Name = "list_counterparty_documents"), Description("List documents for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("CounterpartyDocumentKind filter (optional)")] string? kind = null,
        [Description("DocumentVerificationStatus filter (optional)")] string? verificationStatus = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListCounterpartyDocumentsRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Kind = CounterpartyToolInput.ParseOptionalEnum<CounterpartyDocumentKind>(kind, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        request.VerificationStatus = CounterpartyToolInput.ParseOptionalEnum<DocumentVerificationStatus>(verificationStatus, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListCounterpartyDocumentsAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new { items = response.Items.Select(CounterpartyMapper.MapDocument) });
    }
}

[McpServerToolType]
public static class ListExpiringDocumentsTool
{
    [McpServerTool(Name = "list_expiring_documents"), Description("List non-rejected documents expiring within the next N days.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Days ahead window (1..365)")] int daysAhead = 30,
        [Description("CounterpartyDocumentKind filter (optional)")] string? kind = null,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListExpiringDocumentsRequest
        {
            DaysAhead = Math.Clamp(daysAhead, 1, 365),
            Kind = CounterpartyToolInput.ParseOptionalEnum<CounterpartyDocumentKind>(kind, out var error),
            Pagination = CounterpartyToolInput.Page(page, pageSize),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListExpiringDocumentsAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new
        {
            items = response.Items.Select(CounterpartyMapper.MapDocument),
            page = CounterpartyMapper.MapPage(response.Metadata, request.Pagination.Page, request.Pagination.PageSize),
        });
    }
}

[McpServerToolType]
public static class ImportCounterpartyTool
{
    [McpServerTool(Name = "import_counterparty"), Description(
        "Idempotently import a counterparty with caller-provided ID. Accepts PII inputs but never echoes them.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID to import")] string counterpartyId,
        [Description("CounterpartyKind")] string kind,
        [Description("Display name text or JSON locale map")] string displayName,
        [Description("Roles as JSON array or CSV (optional)")] string? roles = null,
        [Description("VerificationLevel (optional)")] string? verificationLevel = null,
        [Description("DataSource (optional)")] string? dataSource = null,
        [Description("VisibilityMode (optional)")] string? visibilityMode = null,
        [Description("Storefront scope IDs as JSON array or CSV (optional)")] string? storefrontScopeIds = null,
        [Description("Tags as JSON array or CSV (optional)")] string? tags = null,
        [Description("Description text or JSON locale map (optional)")] string? description = null,
        [Description("Parent counterparty ID (optional)")] string? parentCounterpartyId = null,
        [Description("Legal name PII input (optional; not echoed)")] string? legalName = null,
        [Description("Tax ID PII input (optional; not echoed)")] string? taxId = null,
        [Description("Registration number PII input (optional; not echoed)")] string? registrationNumber = null,
        [Description("Email PII input (optional; not echoed)")] string? email = null,
        [Description("Phone PII input (optional; not echoed)")] string? phone = null,
        [Description("Address JSON PII input (optional; not echoed)")] string? addressJson = null,
        [Description("Avatar attachment ID (optional)")] string? avatarAttachmentId = null,
        [Description("Document attachment IDs as JSON array or CSV (optional)")] string? documentAttachmentIds = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var request = CounterpartyCrudToolRequests.Import(
            counterpartyId, kind, displayName, roles, verificationLevel, dataSource, visibilityMode, storefrontScopeIds,
            tags, description, parentCounterpartyId, legalName, taxId, registrationNumber, email, phone, addressJson,
            avatarAttachmentId, documentAttachmentIds, out var error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ImportCounterpartyAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapCounterparty(response));
    }
}

[McpServerToolType]
public static class ListCounterpartyAuditLogTool
{
    [McpServerTool(Name = "list_counterparty_audit_log"), Description("List synthesized audit entries with PII change keys filtered out.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListCounterpartyAuditLogRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Pagination = CounterpartyToolInput.Page(page, pageSize),
        };
        var response = await client.ListCounterpartyAuditLogAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new
        {
            entries = response.Entries.Select(CounterpartyMapper.MapAuditEntry),
            page = CounterpartyMapper.MapPage(response.Metadata, request.Pagination.Page, request.Pagination.PageSize),
        });
    }
}

[McpServerToolType]
public static class GrantCounterpartyAccessTool
{
    [McpServerTool(Name = "grant_counterparty_access"), Description("Grant or update a user's ACL permission on a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("User ID")] string userId,
        [Description("CounterpartyAclPermissionLevel")] string permissionLevel,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!CounterpartyToolInput.TryParseEnum<CounterpartyAclPermissionLevel>(permissionLevel, out var level))
        {
            return McpProtoHelper.FormatError("permissionLevel is invalid");
        }

        var response = await client.GrantCounterpartyAccessAsync(
            new GrantCounterpartyAccessRequest
            {
                CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
                UserId = CounterpartyToolInput.ToGuid(userId),
                PermissionLevel = level,
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapAcl(response));
    }
}

[McpServerToolType]
public static class RevokeCounterpartyAccessTool
{
    [McpServerTool(Name = "revoke_counterparty_access"), Description("Revoke a user's ACL permission on a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("User ID")] string userId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        await client.RevokeCounterpartyAccessAsync(
            new RevokeCounterpartyAccessRequest
            {
                CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
                UserId = CounterpartyToolInput.ToGuid(userId),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { revoked = true, counterpartyId, userId });
    }
}

[McpServerToolType]
public static class ListCounterpartyAclTool
{
    [McpServerTool(Name = "list_counterparty_acl"), Description("List ACL entries for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListCounterpartyAclAsync(
            new ListCounterpartyAclRequest { CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { entries = response.Entries.Select(CounterpartyMapper.MapAcl) });
    }
}

[McpServerToolType]
public static class SubmitForVerificationTool
{
    [McpServerTool(Name = "submit_for_verification"), Description("Submit a counterparty for the next verification level.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("Evidence document refs as JSON array or CSV (optional)")] string? documentMediaRefs = null,
        [Description("Submitter note (optional)")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var request = new SubmitForVerificationRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Note = CounterpartyToolInput.OptionalString(note),
        };
        request.DocumentMediaRefs.Add(CounterpartyToolInput.ParseStringList(documentMediaRefs, nameof(documentMediaRefs), out var error));
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.SubmitForVerificationAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapVerificationAttempt(response));
    }
}

[McpServerToolType]
public static class ApproveVerificationTool
{
    [McpServerTool(Name = "approve_verification"), Description("Approve a pending verification attempt.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Verification attempt ID")] string attemptId,
        [Description("Admin note (optional)")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.ApproveVerificationAsync(
            new ApproveVerificationRequest
            {
                AttemptId = CounterpartyToolInput.ToGuid(attemptId),
                Note = CounterpartyToolInput.OptionalString(note),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapVerificationAttempt(response));
    }
}

[McpServerToolType]
public static class RejectVerificationTool
{
    [McpServerTool(Name = "reject_verification"), Description("Reject a pending verification attempt.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Verification attempt ID")] string attemptId,
        [Description("Admin note or rejection reason (optional)")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.RejectVerificationAsync(
            new RejectVerificationRequest
            {
                AttemptId = CounterpartyToolInput.ToGuid(attemptId),
                Note = CounterpartyToolInput.OptionalString(note),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapVerificationAttempt(response));
    }
}

[McpServerToolType]
public static class ListVerificationAttemptsTool
{
    [McpServerTool(Name = "list_verification_attempts"), Description("List verification attempts for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListVerificationAttemptsAsync(
            new ListVerificationAttemptsRequest { CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { attempts = response.Attempts.Select(CounterpartyMapper.MapVerificationAttempt) });
    }
}

[McpServerToolType]
public static class GetCounterpartyBusinessRelationshipTool
{
    [McpServerTool(Name = "get_counterparty_business_relationship"), Description("Get the derived business relationship snapshot for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.GetCounterpartyBusinessRelationshipAsync(
            new GetCounterpartyBusinessRelationshipRequest { CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapBusinessRelationship(response));
    }
}

internal static class CounterpartyCrudToolOutput
{
    internal static string Json(object value) => JsonSerializer.Serialize(value, McpJsonDefaults.Options);
}

internal static class CounterpartyCrudToolRequests
{
    internal static CreateCounterpartyRequest Create(
        string kind,
        string displayName,
        string? roles,
        string? verificationLevel,
        string? dataSource,
        string? visibilityMode,
        string? storefrontScopeIds,
        string? tags,
        string? description,
        string? parentCounterpartyId,
        string? legalName,
        string? taxId,
        string? registrationNumber,
        string? email,
        string? phone,
        string? addressJson,
        string? avatarAttachmentId,
        string? documentAttachmentIds,
        out string? error)
    {
        var request = new CreateCounterpartyRequest();
        PopulateCreateShared(
            request, kind, displayName, roles, verificationLevel, dataSource, visibilityMode, storefrontScopeIds, tags,
            description, parentCounterpartyId, legalName, taxId, registrationNumber, email, phone, addressJson,
            avatarAttachmentId, documentAttachmentIds, out error);
        return request;
    }

    internal static UpdateCounterpartyRequest Update(
        string id,
        string? displayName,
        string? roles,
        string? status,
        string? verificationLevel,
        string? visibilityMode,
        string? storefrontScopeIds,
        string? tags,
        string? description,
        string? parentCounterpartyId,
        string? legalName,
        string? taxId,
        string? registrationNumber,
        string? email,
        string? phone,
        string? addressJson,
        string? avatarAttachmentId,
        bool clearAvatarAttachment,
        string? documentAttachmentIds,
        out string? error)
    {
        var parsedDisplayName = CounterpartyToolInput.ParseLocalizedText(displayName, nameof(displayName), required: false, out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedDescription = CounterpartyToolInput.ParseLocalizedText(description, nameof(description), required: false, out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedAddress = CounterpartyToolInput.ParseAddress(addressJson, out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedRoles = CounterpartyToolInput.ParseEnumList<CounterpartyRole>(roles, nameof(roles), out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedStorefrontScope = CounterpartyToolInput.ParseGuidList(storefrontScopeIds, nameof(storefrontScopeIds), out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedTags = CounterpartyToolInput.ParseStringList(tags, nameof(tags), out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedDocumentAttachmentIds = CounterpartyToolInput.ParseGuidList(documentAttachmentIds, nameof(documentAttachmentIds), out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedStatus = CounterpartyToolInput.ParseOptionalEnum<CounterpartyStatus>(status, out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedVerificationLevel = CounterpartyToolInput.ParseOptionalEnum<VerificationLevel>(verificationLevel, out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var parsedVisibilityMode = CounterpartyToolInput.ParseOptionalEnum<VisibilityMode>(visibilityMode, out error);
        if (error is not null)
        {
            return new UpdateCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        }

        var request = new UpdateCounterpartyRequest
        {
            Id = CounterpartyToolInput.ToGuid(id),
            DisplayName = parsedDisplayName,
            LegalName = CounterpartyToolInput.OptionalString(legalName),
            TaxId = CounterpartyToolInput.OptionalString(taxId),
            RegistrationNumber = CounterpartyToolInput.OptionalString(registrationNumber),
            Status = parsedStatus,
            VerificationLevel = parsedVerificationLevel,
            VisibilityMode = parsedVisibilityMode,
            Description = parsedDescription,
            ParentCounterpartyId = CounterpartyToolInput.ToGuidOrNull(parentCounterpartyId),
            Email = CounterpartyToolInput.OptionalString(email),
            Phone = CounterpartyToolInput.OptionalString(phone),
            Address = parsedAddress,
            AvatarAttachmentId = CounterpartyToolInput.ToGuidOrNull(avatarAttachmentId),
            ClearAvatarAttachment = clearAvatarAttachment,
        };
        request.Roles.Add(parsedRoles);
        request.StorefrontScope.Add(parsedStorefrontScope);
        request.Tags.Add(parsedTags);
        request.DocumentAttachmentIds.Add(parsedDocumentAttachmentIds);
        return request;
    }

    internal static ImportCounterpartyRequest Import(
        string id,
        string kind,
        string displayName,
        string? roles,
        string? verificationLevel,
        string? dataSource,
        string? visibilityMode,
        string? storefrontScopeIds,
        string? tags,
        string? description,
        string? parentCounterpartyId,
        string? legalName,
        string? taxId,
        string? registrationNumber,
        string? email,
        string? phone,
        string? addressJson,
        string? avatarAttachmentId,
        string? documentAttachmentIds,
        out string? error)
    {
        var request = new ImportCounterpartyRequest { Id = CounterpartyToolInput.ToGuid(id) };
        PopulateImportShared(
            request, kind, displayName, roles, verificationLevel, dataSource, visibilityMode, storefrontScopeIds, tags,
            description, parentCounterpartyId, legalName, taxId, registrationNumber, email, phone, addressJson,
            avatarAttachmentId, documentAttachmentIds, out error);
        return request;
    }

    private static void PopulateCreateShared(
        CreateCounterpartyRequest request,
        string kind,
        string displayName,
        string? roles,
        string? verificationLevel,
        string? dataSource,
        string? visibilityMode,
        string? storefrontScopeIds,
        string? tags,
        string? description,
        string? parentCounterpartyId,
        string? legalName,
        string? taxId,
        string? registrationNumber,
        string? email,
        string? phone,
        string? addressJson,
        string? avatarAttachmentId,
        string? documentAttachmentIds,
        out string? error)
    {
        if (!CounterpartyToolInput.TryParseEnum<CounterpartyKind>(kind, out var parsedKind))
        {
            error = "kind is invalid";
            return;
        }

        request.Kind = parsedKind;
        request.DisplayName = CounterpartyToolInput.ParseLocalizedText(displayName, nameof(displayName), required: true, out error);
        if (error is not null)
        {
            return;
        }

        request.LegalName = CounterpartyToolInput.OptionalString(legalName);
        request.TaxId = CounterpartyToolInput.OptionalString(taxId);
        request.RegistrationNumber = CounterpartyToolInput.OptionalString(registrationNumber);
        request.Email = CounterpartyToolInput.OptionalString(email);
        request.Phone = CounterpartyToolInput.OptionalString(phone);
        request.Description = CounterpartyToolInput.ParseLocalizedText(description, nameof(description), required: false, out error);
        if (error is not null)
        {
            return;
        }

        request.Address = CounterpartyToolInput.ParseAddress(addressJson, out error);
        if (error is not null)
        {
            return;
        }

        request.ParentCounterpartyId = CounterpartyToolInput.ToGuidOrNull(parentCounterpartyId);
        request.AvatarAttachmentId = CounterpartyToolInput.ToGuidOrNull(avatarAttachmentId);

        request.Roles.Add(CounterpartyToolInput.ParseEnumList<CounterpartyRole>(roles, nameof(roles), out error));
        if (error is not null)
        {
            return;
        }

        request.StorefrontScope.Add(CounterpartyToolInput.ParseGuidList(storefrontScopeIds, nameof(storefrontScopeIds), out error));
        if (error is not null)
        {
            return;
        }

        request.Tags.Add(CounterpartyToolInput.ParseStringList(tags, nameof(tags), out error));
        if (error is not null)
        {
            return;
        }

        request.DocumentAttachmentIds.Add(CounterpartyToolInput.ParseGuidList(documentAttachmentIds, nameof(documentAttachmentIds), out error));
        if (error is not null)
        {
            return;
        }

        request.VerificationLevel = CounterpartyToolInput.ParseOptionalEnum<VerificationLevel>(verificationLevel, out error);
        if (error is not null)
        {
            return;
        }

        request.DataSource = CounterpartyToolInput.ParseOptionalEnum<DataSource>(dataSource, out error);
        if (error is not null)
        {
            return;
        }

        request.VisibilityMode = CounterpartyToolInput.ParseOptionalEnum<VisibilityMode>(visibilityMode, out error);
    }

    private static void PopulateImportShared(
        ImportCounterpartyRequest request,
        string kind,
        string displayName,
        string? roles,
        string? verificationLevel,
        string? dataSource,
        string? visibilityMode,
        string? storefrontScopeIds,
        string? tags,
        string? description,
        string? parentCounterpartyId,
        string? legalName,
        string? taxId,
        string? registrationNumber,
        string? email,
        string? phone,
        string? addressJson,
        string? avatarAttachmentId,
        string? documentAttachmentIds,
        out string? error)
    {
        if (!CounterpartyToolInput.TryParseEnum<CounterpartyKind>(kind, out var parsedKind))
        {
            error = "kind is invalid";
            return;
        }

        request.Kind = parsedKind;
        request.DisplayName = CounterpartyToolInput.ParseLocalizedText(displayName, nameof(displayName), required: true, out error);
        if (error is not null)
        {
            return;
        }

        request.LegalName = CounterpartyToolInput.OptionalString(legalName);
        request.TaxId = CounterpartyToolInput.OptionalString(taxId);
        request.RegistrationNumber = CounterpartyToolInput.OptionalString(registrationNumber);
        request.Email = CounterpartyToolInput.OptionalString(email);
        request.Phone = CounterpartyToolInput.OptionalString(phone);
        request.Description = CounterpartyToolInput.ParseLocalizedText(description, nameof(description), required: false, out error);
        if (error is not null)
        {
            return;
        }

        request.Address = CounterpartyToolInput.ParseAddress(addressJson, out error);
        if (error is not null)
        {
            return;
        }

        request.ParentCounterpartyId = CounterpartyToolInput.ToGuidOrNull(parentCounterpartyId);
        request.AvatarAttachmentId = CounterpartyToolInput.ToGuidOrNull(avatarAttachmentId);

        request.Roles.Add(CounterpartyToolInput.ParseEnumList<CounterpartyRole>(roles, nameof(roles), out error));
        if (error is not null)
        {
            return;
        }

        request.StorefrontScope.Add(CounterpartyToolInput.ParseGuidList(storefrontScopeIds, nameof(storefrontScopeIds), out error));
        if (error is not null)
        {
            return;
        }

        request.Tags.Add(CounterpartyToolInput.ParseStringList(tags, nameof(tags), out error));
        if (error is not null)
        {
            return;
        }

        request.DocumentAttachmentIds.Add(CounterpartyToolInput.ParseGuidList(documentAttachmentIds, nameof(documentAttachmentIds), out error));
        if (error is not null)
        {
            return;
        }

        request.VerificationLevel = CounterpartyToolInput.ParseOptionalEnum<VerificationLevel>(verificationLevel, out error);
        if (error is not null)
        {
            return;
        }

        request.DataSource = CounterpartyToolInput.ParseOptionalEnum<DataSource>(dataSource, out error);
        if (error is not null)
        {
            return;
        }

        request.VisibilityMode = CounterpartyToolInput.ParseOptionalEnum<VisibilityMode>(visibilityMode, out error);
    }
}
