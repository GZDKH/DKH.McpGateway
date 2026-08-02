using CurrencyCrud = DKH.ReferenceService.Contracts.Api.CurrenciesCrud.V1;
using CurrencyMgmt = DKH.ReferenceService.Contracts.Reference.Api.CurrencyManagement.v1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageCurrencyTool
{
    [McpServerTool(Name = "manage_currency"), Description(
        "Manage currencies: create, update, delete, get, or list. " +
        "For create/update: provide currency JSON with fields: code, rate, symbol, customFormatting, " +
        "isPrimary, published, displayOrder, translations [{languageCode, name}]. " +
        "Update/delete require both stableId and positive expectedAuthorityVersion returned by get/list. " +
        "For get: provide currency code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient managementClient,
        CurrencyCrud.CurrenciesCrudService.CurrenciesCrudServiceClient crudClient,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Currency JSON (for create/update)")] string? json = null,
        [Description("Currency code (for get, e.g. 'USD', 'RUB')")] string? code = null,
        [Description("Stable currency ID returned by get/list; required for update/delete")]
        string? stableId = null,
        [Description("Authority version returned by get/list; required and must be positive for update/delete")]
        long? expectedAuthorityVersion = null,
        [Description("Search text (for list)")] string? search = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        [Description("Language code to filter translations (for get/list)")] string? language = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "create" or "update" => await ManageAsync(
                crudClient, apiKeyContext, action, json, stableId, expectedAuthorityVersion, cancellationToken),
            "delete" => await DeleteAsync(
                crudClient, apiKeyContext, stableId, expectedAuthorityVersion, cancellationToken),
            "get" => await GetAsync(managementClient, apiKeyContext, code, language, cancellationToken),
            "list" => await ListAsync(managementClient, apiKeyContext, search, page, pageSize, language, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: create, update, delete, get, or list"),
        };
    }

    private static async Task<string> ManageAsync(
        CurrencyCrud.CurrenciesCrudService.CurrenciesCrudServiceClient crudClient,
        IApiKeyContext ctx,
        string action,
        string? json,
        string? stableId,
        long? expectedAuthorityVersion,
        CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update");
        }

        if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            var request = McpProtoHelper.Parser.Parse<CurrencyCrud.CreateCurrencyRequest>(json);
            var createResponse = await crudClient.CreateCurrencyAsync(request, cancellationToken: ct);
            return McpProtoHelper.Formatter.Format(createResponse.Currency);
        }

        if (!ReferenceAuthorityVersionGuard.TryGetMutationIdentity(
                stableId, expectedAuthorityVersion, out var id, out var expected, out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var update = McpProtoHelper.Parser.Parse<CurrencyCrud.UpdateCurrencyRequest>(json);
        if (string.IsNullOrWhiteSpace(update.Code))
        {
            return McpProtoHelper.FormatError("currency code is required in json for update");
        }

        update.Id = id;
        update.ExpectedAuthorityVersion = expected;

        var updateResponse = await crudClient.UpdateCurrencyAsync(update, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(updateResponse.Currency);
    }

    private static async Task<string> DeleteAsync(
        CurrencyCrud.CurrenciesCrudService.CurrenciesCrudServiceClient crudClient,
        IApiKeyContext ctx,
        string? stableId,
        long? expectedAuthorityVersion,
        CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (!ReferenceAuthorityVersionGuard.TryGetMutationIdentity(
                stableId, expectedAuthorityVersion, out var id, out var expected, out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        await crudClient.DeleteCurrencyAsync(new CurrencyCrud.DeleteCurrencyRequest
        {
            Id = id,
            ExpectedAuthorityVersion = expected,
        }, cancellationToken: ct);
        return McpProtoHelper.FormatOk();
    }

    private static async Task<string> GetAsync(
        CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient client,
        IApiKeyContext ctx,
        string? code,
        string? language,
        CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new CurrencyMgmt.GetCurrencyRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(response);
    }

    private static async Task<string> ListAsync(
        CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient client,
        IApiKeyContext ctx,
        string? search,
        int? page,
        int? pageSize,
        string? language,
        CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new CurrencyMgmt.ListCurrenciesRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
