using WeightCrud = DKH.ReferenceService.Contracts.Api.WeightsCrud.V1;
using WeightMgmt = DKH.ReferenceService.Contracts.Reference.Api.WeightManagement.v2;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageWeightTool
{
    [McpServerTool(Name = "manage_weight"), Description(
        "Manage weights: create, update, delete, get, or list. " +
        "For create/update: provide weight JSON with fields: code, ratioToPrimary, isPrimary, " +
        "published, displayOrder, translations [{languageCode, name}]. " +
        "Update/delete require both stableId and positive expectedAuthorityVersion returned by get/list. " +
        "For get: provide weight code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WeightMgmt.WeightManagementService.WeightManagementServiceClient managementClient,
        WeightCrud.WeightsCrudService.WeightsCrudServiceClient crudClient,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Weight JSON (for create/update)")] string? json = null,
        [Description("Weight code (for get)")] string? code = null,
        [Description("Stable weight ID returned by get/list; required for update/delete")]
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
        WeightCrud.WeightsCrudService.WeightsCrudServiceClient crudClient,
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
            var request = McpProtoHelper.Parser.Parse<WeightCrud.CreateWeightRequest>(json);
            var createResponse = await crudClient.CreateWeightAsync(request, cancellationToken: ct);
            return McpProtoHelper.Formatter.Format(createResponse.Weight);
        }

        if (!ReferenceAuthorityVersionGuard.TryGetMutationIdentity(
                stableId, expectedAuthorityVersion, out var id, out var expected, out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var update = McpProtoHelper.Parser.Parse<WeightCrud.UpdateWeightRequest>(json);
        if (string.IsNullOrWhiteSpace(update.Code))
        {
            return McpProtoHelper.FormatError("weight code is required in json for update");
        }

        update.Id = id;
        update.ExpectedAuthorityVersion = expected;

        var updateResponse = await crudClient.UpdateWeightAsync(update, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(updateResponse.Weight);
    }

    private static async Task<string> DeleteAsync(
        WeightCrud.WeightsCrudService.WeightsCrudServiceClient crudClient,
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

        await crudClient.DeleteWeightAsync(new WeightCrud.DeleteWeightRequest
        {
            Id = id,
            ExpectedAuthorityVersion = expected,
        }, cancellationToken: ct);
        return McpProtoHelper.FormatOk();
    }

    private static async Task<string> GetAsync(
        WeightMgmt.WeightManagementService.WeightManagementServiceClient client,
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
            new WeightMgmt.GetWeightRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(response);
    }

    private static async Task<string> ListAsync(
        WeightMgmt.WeightManagementService.WeightManagementServiceClient client,
        IApiKeyContext ctx,
        string? search,
        int? page,
        int? pageSize,
        string? language,
        CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new WeightMgmt.ListWeightsRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
