using DKH.CustomsService.Contracts.Customs.Api.Declarations.v1;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class GetCustomsDeclarationTool
{
    [McpServerTool(Name = "get_customs_declaration"), Description(
        "Get a single customs declaration by ID. " +
        "Returns filing status, countries, duties, related fulfillment/shipment IDs, declaration items, and certificates.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeclarationsService.DeclarationsServiceClient client,
        [Description("Customs declaration ID (GUID)")] string declarationId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(declarationId))
        {
            return McpProtoHelper.FormatError("declarationId is required");
        }

        var declaration = await client.GetDeclarationAsync(
            new GetDeclarationRequest { DeclarationId = new GuidValue(declarationId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CustomsMapper.MapDeclaration(declaration), McpJsonDefaults.Options);
    }
}
