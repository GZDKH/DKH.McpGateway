using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class SetSourcingOfferStatusTool
{
    [McpServerTool(Name = "set_sourcing_offer_status"), Description(
        "Change the status of a sourcing offer. " +
        "Valid actions: Activate, Expire, Archive.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Sourcing offer ID (GUID)")] string id,
        [Description("Status action to perform (Activate, Expire, Archive)")] string action,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return McpProtoHelper.FormatError("action is required");
        }

        var offer = await client.SetSourcingOfferStatusAsync(
            new SetSourcingOfferStatusRequest
            {
                Id = new GuidValue(id),
                Action = Enum.Parse<SourcingOfferStatusAction>(action, ignoreCase: true),
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSourcingOfferTool.MapSourcingOffer(offer), McpJsonDefaults.Options);
    }
}
