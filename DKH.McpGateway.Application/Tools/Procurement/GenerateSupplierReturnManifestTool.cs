using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GenerateSupplierReturnManifestTool
{
    [McpServerTool(Name = "generate_supplier_return_manifest"), Description(
        "Generate a shipping manifest for a supplier return order. " +
        "Assigns a manifest number and transitions the return to manifest-generated status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Supplier return order ID (GUID)")] string id,
        [Description("Manifest number to assign")] string manifestNumber,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(manifestNumber))
        {
            return McpProtoHelper.FormatError("manifestNumber is required");
        }

        var order = await client.GenerateSupplierReturnManifestAsync(
            new GenerateSupplierReturnManifestRequest
            {
                Id = new GuidValue(id),
                ManifestNumber = manifestNumber,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSupplierReturnOrderTool.MapSupplierReturn(order), McpJsonDefaults.Options);
    }
}
