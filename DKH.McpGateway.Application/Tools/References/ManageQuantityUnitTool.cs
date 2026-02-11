using DKH.ReferenceService.Contracts.Reference.Api.QuantityUnitManagement.v1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageQuantityUnitTool
{
    [McpServerTool(Name = "manage_quantity_unit"), Description(
        "Manage quantity units: create, update, delete, get, or list. " +
        "For create/update: provide quantity unit JSON with fields: code, isDefault, " +
        "published, displayOrder, translations [{languageCode, name, plural, description}]. " +
        "For delete/get: provide quantity unit code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Quantity unit JSON (for create/update)")] string? json = null,
        [Description("Quantity unit code (for delete/get)")] string? code = null,
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
        QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update");
        }

        var data = McpProtoHelper.Parser.Parse<QuantityUnitData>(json);
        var request = new ManageQuantityUnitRequest { Data = data };

        var response = action.ToLowerInvariant() switch
        {
            "create" => await client.CreateAsync(request, cancellationToken: ct),
            "update" => await client.UpdateAsync(request, cancellationToken: ct),
            _ => await client.UpdateAsync(request, cancellationToken: ct),
        };

        return McpProtoHelper.FormatManageResponse(response.Code, response.Errors);
    }

    private static async Task<string> DeleteAsync(
        QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        var response = await client.DeleteAsync(new DeleteQuantityUnitRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatManageResponse(response.Code, response.Errors);
    }

    private static async Task<string> GetAsync(
        QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
        IApiKeyContext ctx, string? code, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new GetQuantityUnitRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.FormatGetResponse(response.Found, response.Data);
    }

    private static async Task<string> ListAsync(
        QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListQuantityUnitsRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
