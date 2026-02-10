using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.PackageManagement.v1;

namespace DKH.McpGateway.Application.Tools.PackageTypes;

[McpServerToolType]
public static class ManagePackageTool
{
    [McpServerTool(Name = "manage_package"), Description(
        "Manage packages: create, update, upsert, delete, get, or list. " +
        "For create/update/upsert: provide package JSON with fields: code, name, quantityUnitCode. " +
        "For delete/get: provide package code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PackageManagementService.PackageManagementServiceClient client,
        [Description("Action: create, update, upsert, delete, get, or list")] string action,
        [Description("Package JSON (for create/update/upsert)")] string? json = null,
        [Description("Package code (for delete/get)")] string? code = null,
        [Description("Search text (for list)")] string? search = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "create" or "update" or "upsert" => await ManageAsync(client, apiKeyContext, action, json, cancellationToken),
            "delete" => await DeleteAsync(client, apiKeyContext, code, cancellationToken),
            "get" => await GetAsync(client, apiKeyContext, code, cancellationToken),
            "list" => await ListAsync(client, apiKeyContext, search, page, pageSize, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: create, update, upsert, delete, get, or list"),
        };
    }

    private static async Task<string> ManageAsync(
        PackageManagementService.PackageManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update/upsert");
        }

        var data = McpProtoHelper.Parser.Parse<PackageData>(json);
        var request = new ManagePackageRequest { Data = data };

        var response = action.ToLowerInvariant() switch
        {
            "create" => await client.CreateAsync(request, cancellationToken: ct),
            "update" => await client.UpdateAsync(request, cancellationToken: ct),
            _ => await client.UpsertAsync(request, cancellationToken: ct),
        };

        return McpProtoHelper.FormatManageResponse(response.Success, response.Action, response.Code, response.Errors);
    }

    private static async Task<string> DeleteAsync(
        PackageManagementService.PackageManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        var response = await client.DeleteAsync(new DeletePackageRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatManageResponse(response.Success, response.Action, response.Code, response.Errors);
    }

    private static async Task<string> GetAsync(
        PackageManagementService.PackageManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new GetPackageRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatGetResponse(response.Found, response.Data);
    }

    private static async Task<string> ListAsync(
        PackageManagementService.PackageManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListPackagesRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
