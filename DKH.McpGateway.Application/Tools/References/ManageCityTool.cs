using DKH.ReferenceService.Contracts.Reference.Api.CityManagement.v1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageCityTool
{
    [McpServerTool(Name = "manage_city"), Description(
        "Manage cities: create, update, delete, get, or list. " +
        "For create/update: provide city JSON with fields: code, country, state, " +
        "published, displayOrder, translations [{languageCode, name}]. " +
        "For delete/get: provide city code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CityManagementService.CityManagementServiceClient client,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("City JSON (for create/update)")] string? json = null,
        [Description("City code (for delete/get)")] string? code = null,
        [Description("Search text (for list)")] string? search = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        [Description("Language code to filter translations (for get/list)")] string? language = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "create" or "update" => await ManageAsync(client, apiKeyContext, action, json, cancellationToken),
            "delete" => await DeleteAsync(client, apiKeyContext, code, cancellationToken),
            "get" => await GetAsync(client, apiKeyContext, code, language, cancellationToken),
            "list" => await ListAsync(client, apiKeyContext, search, page, pageSize, language, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: create, update, delete, get, or list"),
        };
    }

    private static async Task<string> ManageAsync(
        CityManagementService.CityManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update");
        }

        var data = McpProtoHelper.Parser.Parse<CityData>(json);
        var request = new ManageCityRequest { Data = data };

        var response = action.ToLowerInvariant() switch
        {
            "create" => await client.CreateAsync(request, cancellationToken: ct),
            "update" => await client.UpdateAsync(request, cancellationToken: ct),
            _ => await client.UpdateAsync(request, cancellationToken: ct),
        };

        return McpProtoHelper.FormatManageResponse(response.Success, response.Action, response.Code, response.Errors);
    }

    private static async Task<string> DeleteAsync(
        CityManagementService.CityManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        var response = await client.DeleteAsync(new DeleteCityRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatManageResponse(response.Success, response.Action, response.Code, response.Errors);
    }

    private static async Task<string> GetAsync(
        CityManagementService.CityManagementServiceClient client,
        IApiKeyContext ctx, string? code, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new GetCityRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.FormatGetResponse(response.Found, response.Data);
    }

    private static async Task<string> ListAsync(
        CityManagementService.CityManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListCitiesRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
