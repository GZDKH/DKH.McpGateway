using DKH.CustomsService.Contracts.NomenclatureSystem.Api.Crud.V2;
using DKH.CustomsService.Contracts.NomenclatureSystem.Models.V2;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class CreateNomenclatureSystemTool
{
    [McpServerTool(Name = "create_nomenclature_system"), Description(
        "Create a nomenclature system. isoCountryCodesJson: JSON string array. translationsJson: JSON array [{\"lang\":\"en\",\"name\":\"China\",\"description\":\"China HS\"}].")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient client,
        [Description("System code")] string code,
        [Description("Region enum name")] string region,
        [Description("Digit count")] int digitsCount,
        [Description("Source URL")] string sourceUrl,
        [Description("Current revision")] string currentRevision,
        [Description("ISO country code JSON array")] string? isoCountryCodesJson = null,
        [Description("Translations JSON array")] string? translationsJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        if (!CustomsToolInput.TryParseEnum<Region>(region, out var parsedRegion))
        {
            return McpProtoHelper.FormatError("region is invalid");
        }

        var request = new CreateNomenclatureSystemRequest
        {
            Code = code,
            Region = parsedRegion,
            DigitsCount = digitsCount,
            SourceUrl = sourceUrl ?? string.Empty,
            CurrentRevision = currentRevision ?? string.Empty,
        };

        var countriesError = CustomsToolInput.PopulateIsoCountryCodes(request.IsoCountryCodes, isoCountryCodesJson);
        if (countriesError is not null)
        {
            return McpProtoHelper.FormatError(countriesError);
        }

        var translationsError = CustomsToolInput.PopulateNomenclatureTranslations(request.Translations, translationsJson);
        if (translationsError is not null)
        {
            return McpProtoHelper.FormatError(translationsError);
        }

        var model = await client.CreateNomenclatureSystemAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapNomenclatureSystem(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class UpdateNomenclatureSystemTool
{
    [McpServerTool(Name = "update_nomenclature_system"), Description("Update nomenclature system source/revision/countries/translations.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient client,
        [Description("Nomenclature system ID")] string id,
        [Description("Source URL")] string? sourceUrl = null,
        [Description("Current revision")] string? currentRevision = null,
        [Description("ISO country code JSON array")] string? isoCountryCodesJson = null,
        [Description("Translations JSON array")] string? translationsJson = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateNomenclatureSystemRequest
        {
            Id = id,
            SourceUrl = sourceUrl ?? string.Empty,
            CurrentRevision = currentRevision ?? string.Empty,
        };

        var countriesError = CustomsToolInput.PopulateIsoCountryCodes(request.IsoCountryCodes, isoCountryCodesJson);
        if (countriesError is not null)
        {
            return McpProtoHelper.FormatError(countriesError);
        }

        var translationsError = CustomsToolInput.PopulateNomenclatureTranslations(request.Translations, translationsJson);
        if (translationsError is not null)
        {
            return McpProtoHelper.FormatError(translationsError);
        }

        var model = await client.UpdateNomenclatureSystemAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapNomenclatureSystem(model), McpJsonDefaults.Options);
    }
}
