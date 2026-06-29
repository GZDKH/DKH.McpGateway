using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class GetReceivingActTool
{
    [McpServerTool(Name = "get_receiving_act"), Description(
        "Get a receiving act by its ID. " +
        "Returns the act linked to a purchase order, the signing user, and sign timestamp.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Receiving act ID (GUID)")] string receivingActId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(receivingActId))
        {
            return McpProtoHelper.FormatError("receivingActId is required");
        }

        var act = await client.GetReceivingActAsync(
            new GetReceivingActRequest { ReceivingActId = new GuidValue(receivingActId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapReceivingAct(act), McpJsonDefaults.Options);
    }

    internal static object MapReceivingAct(ReceivingActModel act) => new
    {
        id = act.Id?.Value,
        purchaseOrderId = act.PurchaseOrderId?.Value,
        signedBy = act.SignedBy?.Value,
        signedAt = act.SignedAt?.ToDateTimeOffset().ToString("O"),
    };
}
