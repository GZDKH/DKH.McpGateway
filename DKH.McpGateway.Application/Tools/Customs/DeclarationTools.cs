using DKH.CustomsService.Contracts.Customs.Api.Declarations.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class CreateCustomsDeclarationTool
{
    [McpServerTool(Name = "create_customs_declaration"), Description(
        "Create a customs declaration. itemsJson: JSON array [{\"hsCode\":\"0902\",\"description\":\"Tea\",\"quantity\":1,\"declaredValue\":10.0}].")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("DeclarationType enum name")] string type,
        [Description("Destination country code")] string destinationCountry,
        [Description("ISO-4217 currency code")] string currency,
        [Description("Declaration item JSON array")] string itemsJson,
        [Description("Optional origin country code")] string? originCountry = null,
        [Description("Optional fulfillment ID")] string? fulfillmentId = null,
        [Description("Optional shipment ID")] string? shipmentId = null,
        [Description("Optional broker partner ID")] string? brokerPartnerId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!CustomsToolInput.TryParseEnum<DeclarationType>(type, out var parsedType))
        {
            return McpProtoHelper.FormatError("type is invalid");
        }

        if (string.IsNullOrWhiteSpace(destinationCountry))
        {
            return McpProtoHelper.FormatError("destinationCountry is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        var request = new CreateDeclarationRequest
        {
            Type = parsedType,
            DestinationCountry = destinationCountry,
            OriginCountry = originCountry,
            Currency = currency,
            FulfillmentId = ToGuidValue(fulfillmentId),
            ShipmentId = ToGuidValue(shipmentId),
            BrokerPartnerId = ToGuidValue(brokerPartnerId),
        };

        var parseError = CustomsToolInput.PopulateDeclarationItems(request.Items, itemsJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var declaration = await client.CreateDeclarationAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapDeclaration(declaration), McpJsonDefaults.Options);
    }

    private static GuidValue? ToGuidValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new GuidValue(value);
}

[McpServerToolType]
public static class UpdateCustomsDeclarationItemsTool
{
    [McpServerTool(Name = "update_customs_declaration_items"), Description("Replace declaration items for a draft/amended declaration.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("Customs declaration ID")] string declarationId,
        [Description("Declaration item JSON array")] string itemsJson,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(declarationId))
        {
            return McpProtoHelper.FormatError("declarationId is required");
        }

        var request = new UpdateDeclarationItemsRequest { DeclarationId = new GuidValue(declarationId) };
        var parseError = CustomsToolInput.PopulateDeclarationItems(request.Items, itemsJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var declaration = await client.UpdateDeclarationItemsAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapDeclaration(declaration), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class AttachCustomsCertificateTool
{
    [McpServerTool(Name = "attach_customs_certificate"), Description("Attach a certificate document reference to a customs declaration.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("Customs declaration ID")] string declarationId,
        [Description("CertificateType enum name")] string type,
        [Description("Issuing authority")] string issuingAuthority,
        [Description("Document reference")] string documentRef,
        [Description("Optional ISO-8601 valid-from timestamp")] string? validFrom = null,
        [Description("Optional ISO-8601 valid-until timestamp")] string? validUntil = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(declarationId))
        {
            return McpProtoHelper.FormatError("declarationId is required");
        }

        if (!CustomsToolInput.TryParseEnum<CertificateType>(type, out var parsedType))
        {
            return McpProtoHelper.FormatError("type is invalid");
        }

        if (string.IsNullOrWhiteSpace(issuingAuthority))
        {
            return McpProtoHelper.FormatError("issuingAuthority is required");
        }

        if (string.IsNullOrWhiteSpace(documentRef))
        {
            return McpProtoHelper.FormatError("documentRef is required");
        }

        var request = new AttachCertificateRequest
        {
            DeclarationId = new GuidValue(declarationId),
            Type = parsedType,
            IssuingAuthority = issuingAuthority,
            DocumentRef = documentRef,
            ValidFrom = CustomsToolInput.ParseTimestamp(validFrom),
            ValidUntil = CustomsToolInput.ParseTimestamp(validUntil),
        };

        var declaration = await client.AttachCertificateAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapDeclaration(declaration), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class SubmitCustomsDeclarationTool
{
    [McpServerTool(Name = "submit_customs_declaration"), Description("Submit a customs declaration with an optional filing reference.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("Customs declaration ID")] string declarationId,
        [Description("Optional external filing reference")] string? filingReference = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(declarationId))
        {
            return McpProtoHelper.FormatError("declarationId is required");
        }

        var declaration = await client.SubmitDeclarationAsync(
            new SubmitDeclarationRequest { DeclarationId = new GuidValue(declarationId), FilingReference = filingReference },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapDeclaration(declaration), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class UpdateCustomsDeclarationStatusTool
{
    [McpServerTool(Name = "update_customs_declaration_status"), Description("Advance or reject a customs declaration status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("Customs declaration ID")] string declarationId,
        [Description("DeclarationStatus enum name")] string nextStatus,
        [Description("Required when nextStatus is Rejected")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(declarationId))
        {
            return McpProtoHelper.FormatError("declarationId is required");
        }

        if (!CustomsToolInput.TryParseEnum<DeclarationStatus>(nextStatus, out var parsedStatus))
        {
            return McpProtoHelper.FormatError("nextStatus is invalid");
        }

        if (parsedStatus == DeclarationStatus.Rejected && string.IsNullOrWhiteSpace(reason))
        {
            return McpProtoHelper.FormatError("reason is required when nextStatus is Rejected");
        }

        var declaration = await client.UpdateDeclarationStatusAsync(
            new UpdateDeclarationStatusRequest
            {
                DeclarationId = new GuidValue(declarationId),
                NextStatus = parsedStatus,
                Reason = reason,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapDeclaration(declaration), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ListCustomsDeclarationsTool
{
    [McpServerTool(Name = "list_customs_declarations"), Description("List customs declarations with optional fulfillment, shipment, and status filters.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        [Description("Optional fulfillment ID")] string? fulfillmentId = null,
        [Description("Optional shipment ID")] string? shipmentId = null,
        [Description("Optional DeclarationStatus enum name")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListDeclarationsRequest
        {
            Page = page,
            PageSize = pageSize,
            Status = CustomsToolInput.ParseEnumOrDefault<DeclarationStatus>(status),
        };
        if (!string.IsNullOrWhiteSpace(fulfillmentId))
        {
            request.FulfillmentId = new GuidValue(fulfillmentId);
        }

        if (!string.IsNullOrWhiteSpace(shipmentId))
        {
            request.ShipmentId = new GuidValue(shipmentId);
        }

        var response = await client.ListDeclarationsAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(CustomsMapper.MapDeclaration),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
