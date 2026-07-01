using DKH.CustomsService.Contracts.NationalHsCode.Api.Crud.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Api.Crud.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Models.V2;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class GetWcoHsCodeTool
{
    [McpServerTool(Name = "get_wco_hs_code"), Description("Get a WCO HS code row by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("WCO HS code row ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var code = await client.GetWcoHsCodeAsync(new GetWcoHsCodeRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapWcoHsCode(code), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetWcoHsCodeByCodeTool
{
    [McpServerTool(Name = "get_wco_hs_code_by_code"), Description("Get a WCO HS code row by code and HS revision.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("WCO HS code")] string code,
        [Description("HS revision")] string hsRevision,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        if (string.IsNullOrWhiteSpace(hsRevision))
        {
            return McpProtoHelper.FormatError("hsRevision is required");
        }

        var model = await client.GetWcoHsCodeByCodeAsync(
            new GetWcoHsCodeByCodeRequest { Code = code, HsRevision = hsRevision },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapWcoHsCode(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ListWcoHsCodesTool
{
    [McpServerTool(Name = "list_wco_hs_codes"), Description(
        "List WCO HS codes with optional level, codePrefix, hsRevision, and includeRetired filters.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        [Description("Optional HsCodeLevel enum name")] string? level = null,
        [Description("Optional code prefix")] string? codePrefix = null,
        [Description("Optional HS revision")] string? hsRevision = null,
        [Description("Include retired codes")] bool includeRetired = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListWcoHsCodesAsync(
            new ListWcoHsCodesRequest
            {
                Page = page,
                PageSize = pageSize,
                Level = CustomsToolInput.ParseEnumOrDefault<HsCodeLevel>(level),
                CodePrefix = codePrefix ?? string.Empty,
                HsRevision = hsRevision ?? string.Empty,
                IncludeRetired = includeRetired,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(CustomsMapper.MapWcoHsCode),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetWcoHsHierarchyTool
{
    [McpServerTool(Name = "get_wco_hs_hierarchy"), Description("Get a WCO HS hierarchy by root code and HS revision.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WcoHsCodeCrudService.WcoHsCodeCrudServiceClient client,
        [Description("Root WCO HS code")] string rootCode,
        [Description("HS revision")] string hsRevision,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(rootCode))
        {
            return McpProtoHelper.FormatError("rootCode is required");
        }

        if (string.IsNullOrWhiteSpace(hsRevision))
        {
            return McpProtoHelper.FormatError("hsRevision is required");
        }

        var response = await client.GetWcoHsHierarchyAsync(
            new GetWcoHsHierarchyRequest { RootCode = rootCode, HsRevision = hsRevision },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { roots = response.Roots.Select(CustomsMapper.MapWcoHierarchyNode) }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetNationalHsCodeTool
{
    [McpServerTool(Name = "get_national_hs_code"), Description("Get a national HS code row by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("National HS code row ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var code = await client.GetNationalHsCodeAsync(new GetNationalHsCodeRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapNationalHsCode(code), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetNationalHsCodeByFullCodeTool
{
    [McpServerTool(Name = "get_national_hs_code_by_full_code"), Description("Get a national HS code row by system code and full code.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("Nomenclature system code")] string systemCode,
        [Description("Full national HS code")] string fullCode,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(systemCode))
        {
            return McpProtoHelper.FormatError("systemCode is required");
        }

        if (string.IsNullOrWhiteSpace(fullCode))
        {
            return McpProtoHelper.FormatError("fullCode is required");
        }

        var model = await client.GetNationalHsCodeByFullCodeAsync(
            new GetNationalHsCodeByFullCodeRequest { SystemCode = systemCode, FullCode = fullCode },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapNationalHsCode(model), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ListNationalHsCodesTool
{
    [McpServerTool(Name = "list_national_hs_codes"), Description(
        "List national HS codes with optional systemCode, codePrefix, includeRetired, and asOfUtc filters.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        [Description("Optional nomenclature system code")] string? systemCode = null,
        [Description("Optional full-code prefix")] string? codePrefix = null,
        [Description("Include retired codes")] bool includeRetired = false,
        [Description("Optional ISO-8601 timestamp filter")] string? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListNationalHsCodesRequest
        {
            Page = page,
            PageSize = pageSize,
            SystemCode = systemCode ?? string.Empty,
            CodePrefix = codePrefix ?? string.Empty,
            IncludeRetired = includeRetired,
        };

        var parsedAsOf = CustomsToolInput.ParseTimestamp(asOfUtc);
        if (parsedAsOf is not null)
        {
            request.AsOfUtc = parsedAsOf;
        }

        var response = await client.ListNationalHsCodesAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(CustomsMapper.MapNationalHsCode),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetNationalHsHierarchyTool
{
    [McpServerTool(Name = "get_national_hs_hierarchy"), Description("Get a national HS hierarchy by system code and root code.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NationalHsCodeCrudService.NationalHsCodeCrudServiceClient client,
        [Description("Nomenclature system code")] string systemCode,
        [Description("Root full code")] string rootCode,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(systemCode))
        {
            return McpProtoHelper.FormatError("systemCode is required");
        }

        if (string.IsNullOrWhiteSpace(rootCode))
        {
            return McpProtoHelper.FormatError("rootCode is required");
        }

        var response = await client.GetNationalHsHierarchyAsync(
            new GetNationalHsHierarchyRequest { SystemCode = systemCode, RootCode = rootCode },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { roots = response.Roots.Select(CustomsMapper.MapNationalHierarchyNode) }, McpJsonDefaults.Options);
    }
}
