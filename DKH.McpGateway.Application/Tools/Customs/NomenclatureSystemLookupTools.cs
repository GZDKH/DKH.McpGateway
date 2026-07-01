using DKH.CustomsService.Contracts.NomenclatureSystem.Api.Crud.V2;
using DKH.CustomsService.Contracts.NomenclatureSystem.Models.V2;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class GetNomenclatureSystemTool
{
    [McpServerTool(Name = "get_nomenclature_system"), Description("Get a nomenclature system by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient client,
        [Description("Nomenclature system ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var system = await client.GetNomenclatureSystemAsync(new GetNomenclatureSystemRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapNomenclatureSystem(system), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class GetNomenclatureSystemByCodeTool
{
    [McpServerTool(Name = "get_nomenclature_system_by_code"), Description("Get a nomenclature system by code.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient client,
        [Description("Nomenclature system code")] string code,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        var system = await client.GetNomenclatureSystemByCodeAsync(
            new GetNomenclatureSystemByCodeRequest { Code = code },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapNomenclatureSystem(system), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ListNomenclatureSystemsTool
{
    [McpServerTool(Name = "list_nomenclature_systems"), Description("List nomenclature systems with an optional region filter.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NomenclatureSystemCrudService.NomenclatureSystemCrudServiceClient client,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        [Description("Optional Region enum name")] string? region = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListNomenclatureSystemsAsync(
            new ListNomenclatureSystemsRequest
            {
                Page = page,
                PageSize = pageSize,
                Region = CustomsToolInput.ParseEnumOrDefault<Region>(region),
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(CustomsMapper.MapNomenclatureSystem),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
