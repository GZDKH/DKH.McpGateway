using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class RecordSupplierReturnCreditExpectedTool
{
    [McpServerTool(Name = "record_supplier_return_credit_expected"), Description(
        "Record the expected credit amount for a shipped supplier return. " +
        "Sets the credit amount, currency, and optional due date.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Supplier return order ID (GUID)")] string id,
        [Description("Expected credit amount")] decimal amount,
        [Description("Credit currency code (e.g. USD)")] string currency,
        [Description("Credit due date (ISO 8601, optional)")] string? dueAt = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        var request = new RecordSupplierReturnCreditExpectedRequest
        {
            Id = new GuidValue(id),
            Amount = new DecimalValue(amount),
            Currency = currency,
        };

        if (!string.IsNullOrWhiteSpace(dueAt))
        {
            request.DueAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.Parse(dueAt, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
        }

        var order = await client.RecordSupplierReturnCreditExpectedAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetSupplierReturnOrderTool.MapSupplierReturn(order), McpJsonDefaults.Options);
    }
}
