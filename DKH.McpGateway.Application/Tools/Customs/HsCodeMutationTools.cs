using DKH.CustomsService.Contracts.NationalHsCode.Api.Crud.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Api.Crud.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Models.V2;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class CreateWcoHsCodeTool
{
    [McpServerTool(Name = "create_wco_hs_code"), Description(
        "Create a WCO HS code row. translationsJson: JSON array [{\"lang\":\"en\",\"description\":\"Tea\",\"shortName\":\"Tea\"}].")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("WCO HS code")] string code,
        [Description("HsCodeLevel enum name")] string level,
        [Description("HS revision")] string hsRevision,
        [Description("Optional parent WCO code")] string? parentCode = null,
        [Description("Notes")] string? notes = null,
        [Description("Translations JSON array")] string? translationsJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        if (!CustomsToolInput.TryParseEnum<HsCodeLevel>(level, out var parsedLevel))
        {
            return McpProtoHelper.FormatError("level is invalid");
        }

        if (string.IsNullOrWhiteSpace(hsRevision))
        {
            return McpProtoHelper.FormatError("hsRevision is required");
        }

        var request = new CreateWcoHsCodeRequest
        {
            Code = code,
            Level = parsedLevel,
            ParentCode = parentCode ?? string.Empty,
            HsRevision = hsRevision,
            Notes = notes ?? string.Empty,
        };

        var parseError = CustomsToolInput.PopulateWcoTranslations(request.Translations, translationsJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var model = await client.CreateWcoHsCodeAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapWcoHsCode(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class UpdateWcoHsCodeTool
{
    [McpServerTool(Name = "update_wco_hs_code"), Description("Update WCO HS code notes/translations.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("WCO HS code row ID")] string id,
        [Description("Notes")] string? notes = null,
        [Description("Translations JSON array")] string? translationsJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateWcoHsCodeRequest { Id = id, Notes = notes ?? string.Empty };
        var parseError = CustomsToolInput.PopulateWcoTranslations(request.Translations, translationsJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var model = await client.UpdateWcoHsCodeAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapWcoHsCode(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class RetireWcoHsCodeTool
{
    [McpServerTool(Name = "retire_wco_hs_code"), Description("Retire a WCO HS code row.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("WCO HS code row ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.RetireWcoHsCodeAsync(new RetireWcoHsCodeRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(new { ok = true, id }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ReinstateWcoHsCodeTool
{
    [McpServerTool(Name = "reinstate_wco_hs_code"), Description("Reinstate a retired WCO HS code row.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("WCO HS code row ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.ReinstateWcoHsCodeAsync(new ReinstateWcoHsCodeRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(new { ok = true, id }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class CreateNationalHsCodeTool
{
    [McpServerTool(Name = "create_national_hs_code"), Description(
        "Create a national HS code row. translationsJson: JSON array [{\"lang\":\"zh\",\"description\":\"Tea\",\"shortName\":\"Tea\"}].")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("Nomenclature system code")] string systemCode,
        [Description("WCO base code")] string wcoCode,
        [Description("Extended national code suffix")] string extendedCode,
        [Description("HsCodeLevel enum name")] string level,
        [Description("Optional ISO-8601 valid-from timestamp")] string? validFrom = null,
        [Description("Optional ISO-8601 valid-until timestamp")] string? validUntil = null,
        [Description("Notes")] string? notes = null,
        [Description("Translations JSON array")] string? translationsJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(systemCode))
        {
            return McpProtoHelper.FormatError("systemCode is required");
        }

        if (string.IsNullOrWhiteSpace(wcoCode))
        {
            return McpProtoHelper.FormatError("wcoCode is required");
        }

        if (string.IsNullOrWhiteSpace(extendedCode))
        {
            return McpProtoHelper.FormatError("extendedCode is required");
        }

        if (!CustomsToolInput.TryParseEnum<HsCodeLevel>(level, out var parsedLevel))
        {
            return McpProtoHelper.FormatError("level is invalid");
        }

        var request = new CreateNationalHsCodeRequest
        {
            SystemCode = systemCode,
            WcoCode = wcoCode,
            ExtendedCode = extendedCode,
            Level = parsedLevel,
            ValidFrom = CustomsToolInput.ParseTimestamp(validFrom),
            ValidUntil = CustomsToolInput.ParseTimestamp(validUntil),
            Notes = notes ?? string.Empty,
        };

        var parseError = CustomsToolInput.PopulateNationalTranslations(request.Translations, translationsJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var model = await client.CreateNationalHsCodeAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapNationalHsCode(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class UpdateNationalHsCodeTool
{
    [McpServerTool(Name = "update_national_hs_code"), Description("Update national HS code dates/notes/translations.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("National HS code row ID")] string id,
        [Description("Optional ISO-8601 valid-from timestamp")] string? validFrom = null,
        [Description("Optional ISO-8601 valid-until timestamp")] string? validUntil = null,
        [Description("Notes")] string? notes = null,
        [Description("Translations JSON array")] string? translationsJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateNationalHsCodeRequest
        {
            Id = id,
            ValidFrom = CustomsToolInput.ParseTimestamp(validFrom),
            ValidUntil = CustomsToolInput.ParseTimestamp(validUntil),
            Notes = notes ?? string.Empty,
        };

        var parseError = CustomsToolInput.PopulateNationalTranslations(request.Translations, translationsJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var model = await client.UpdateNationalHsCodeAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapNationalHsCode(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class RetireNationalHsCodeTool
{
    [McpServerTool(Name = "retire_national_hs_code"), Description("Retire a national HS code row.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("National HS code row ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.RetireNationalHsCodeAsync(new RetireNationalHsCodeRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(new { ok = true, id }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ReinstateNationalHsCodeTool
{
    [McpServerTool(Name = "reinstate_national_hs_code"), Description("Reinstate a retired national HS code row.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("National HS code row ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.ReinstateNationalHsCodeAsync(new ReinstateNationalHsCodeRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(new { ok = true, id }, McpJsonDefaults.Options);
    }
}
