using QuantityCrud = DKH.ReferenceService.Contracts.Api.QuantityUnitsCrud.V1;
using QuantityMgmt = DKH.ReferenceService.Contracts.Reference.Api.QuantityUnitManagement.v1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageQuantityUnitTool
{
    [McpServerTool(Name = "manage_quantity_unit"), Description(
        "Manage quantity units: create, update, delete, get, or list. " +
        "For create/update: provide quantity unit JSON with fields: code, isDefault, " +
        "published, displayOrder, translations [{languageCode, name, plural, description}]. " +
        "Update/delete require both stableId and positive expectedAuthorityVersion returned by get/list. " +
        "For get: provide quantity unit code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        QuantityMgmt.QuantityUnitManagementService.QuantityUnitManagementServiceClient managementClient,
        QuantityCrud.QuantityUnitsCrudService.QuantityUnitsCrudServiceClient crudClient,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Quantity unit JSON (for create/update)")] string? json = null,
        [Description("Quantity unit code (for get)")] string? code = null,
        [Description("Stable quantity-unit ID returned by get/list; required for update/delete")]
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
        QuantityCrud.QuantityUnitsCrudService.QuantityUnitsCrudServiceClient crudClient,
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
            var request = McpProtoHelper.Parser.Parse<QuantityCrud.CreateQuantityUnitRequest>(json);
            var createResponse = await crudClient.CreateQuantityUnitAsync(request, cancellationToken: ct);
            return McpProtoHelper.Formatter.Format(createResponse.QuantityUnit);
        }

        if (!ReferenceAuthorityVersionGuard.TryGetMutationIdentity(
                stableId, expectedAuthorityVersion, out var id, out var expected, out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var update = McpProtoHelper.Parser.Parse<QuantityCrud.UpdateQuantityUnitRequest>(json);
        if (string.IsNullOrWhiteSpace(update.Code))
        {
            return McpProtoHelper.FormatError("quantity unit code is required in json for update");
        }

        update.Id = id;
        update.ExpectedAuthorityVersion = expected;

        var updateResponse = await crudClient.UpdateQuantityUnitAsync(update, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(updateResponse.QuantityUnit);
    }

    private static async Task<string> DeleteAsync(
        QuantityCrud.QuantityUnitsCrudService.QuantityUnitsCrudServiceClient crudClient,
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

        await crudClient.DeleteQuantityUnitAsync(new QuantityCrud.DeleteQuantityUnitRequest
        {
            Id = id,
            ExpectedAuthorityVersion = expected,
        }, cancellationToken: ct);
        return McpProtoHelper.FormatOk();
    }

    private static async Task<string> GetAsync(
        QuantityMgmt.QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
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
            new QuantityMgmt.GetQuantityUnitRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(response);
    }

    private static async Task<string> ListAsync(
        QuantityMgmt.QuantityUnitManagementService.QuantityUnitManagementServiceClient client,
        IApiKeyContext ctx,
        string? search,
        int? page,
        int? pageSize,
        string? language,
        CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new QuantityMgmt.ListQuantityUnitsRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
