using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Application.Tools.Procurement;

[McpServerToolType]
public static class CreateCustomCustomerOrderTool
{
    [McpServerTool(Name = "create_custom_customer_order"), Description(
        "Create a new custom customer order for a bespoke product. " +
        "modifications: JSON array [{\"field\":\"...\",\"oldValue\":\"...\",\"newValue\":\"...\",\"notes\":\"...\"}] (at least 1 required). " +
        "The customerId and deliveryAddress are accepted for the record but never returned in responses (PII).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProcurementGrpc.ProcurementClient client,
        [Description("Counterparty ID (GUID)")] string counterpartyId,
        [Description("Customer ID (GUID) — stored but never returned in responses (PII)")] string customerId,
        [Description("Delivery address — stored but never returned in responses (PII)")] string deliveryAddress,
        [Description("Base product catalog reference")] string baseProductCatalogRef,
        [Description("Currency code (e.g. USD)")] string currency,
        [Description("User creating this order (GUID)")] string createdByUserId,
        [Description("Modifications JSON array [{\"field\":\"...\",\"oldValue\":\"...\",\"newValue\":\"...\",\"notes\":\"...\"}]")] string modifications,
        [Description("Estimated price (optional)")] decimal? estimatedPrice = null,
        [Description("Deposit amount (optional)")] decimal? depositAmount = null,
        [Description("Balance amount (optional)")] decimal? balanceAmount = null,
        [Description("Planned delivery date/time (ISO 8601, optional)")] string? plannedDeliveryAt = null,
        [Description("Estimated delivery date (ISO 8601, optional)")] string? estimatedDeliveryDate = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(counterpartyId))
        {
            return McpProtoHelper.FormatError("counterpartyId is required");
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return McpProtoHelper.FormatError("customerId is required");
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            return McpProtoHelper.FormatError("createdByUserId is required");
        }

        if (string.IsNullOrWhiteSpace(modifications))
        {
            return McpProtoHelper.FormatError("modifications is required");
        }

        var request = new CreateCustomCustomerOrderRequest
        {
            CounterpartyId = new GuidValue(counterpartyId),
            CustomerId = new GuidValue(customerId),
            DeliveryAddress = deliveryAddress ?? string.Empty,
            BaseProductCatalogRef = baseProductCatalogRef ?? string.Empty,
            Currency = currency ?? string.Empty,
            CreatedByUserId = new GuidValue(createdByUserId),
        };

        if (estimatedPrice.HasValue)
        {
            request.EstimatedPrice = new DecimalValue(estimatedPrice.Value);
        }

        if (depositAmount.HasValue)
        {
            request.DepositAmount = new DecimalValue(depositAmount.Value);
        }

        if (balanceAmount.HasValue)
        {
            request.BalanceAmount = new DecimalValue(balanceAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(plannedDeliveryAt))
        {
            request.PlannedDeliveryAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.Parse(plannedDeliveryAt, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(estimatedDeliveryDate))
        {
            request.EstimatedDeliveryDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.Parse(estimatedDeliveryDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
        }

        try
        {
            var mods = JsonSerializer.Deserialize<List<JsonElement>>(modifications, McpJsonDefaults.Options);
            if (mods is not null)
            {
                foreach (var mod in mods)
                {
                    var field = mod.GetProperty("field").GetString() ?? string.Empty;
                    var oldVal = mod.TryGetProperty("oldValue", out var ov) ? ov.GetString() ?? string.Empty : string.Empty;
                    var newVal = mod.GetProperty("newValue").GetString() ?? string.Empty;
                    var notes = mod.TryGetProperty("notes", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                    request.Modifications.Add(new CustomOrderModificationInput
                    {
                        Field = field,
                        OldValue = oldVal,
                        NewValue = newVal,
                        Notes = notes,
                    });
                }
            }
        }
        catch (Exception)
        {
            return McpProtoHelper.FormatError("modifications JSON is invalid");
        }

        if (request.Modifications.Count == 0)
        {
            return McpProtoHelper.FormatError("at least one modification is required");
        }

        var order = await client.CreateCustomCustomerOrderAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCustomCustomerOrderTool.MapCustomOrder(order), McpJsonDefaults.Options);
    }
}
