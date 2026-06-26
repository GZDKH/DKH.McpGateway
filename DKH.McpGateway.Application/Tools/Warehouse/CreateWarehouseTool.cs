using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class CreateWarehouseTool
{
    [McpServerTool(Name = "create_warehouse"), Description(
        "Create a new warehouse. " +
        "displayName is a JSON array of {\"lang\":\"en\",\"name\":\"...\"} objects. " +
        "addressLines is a JSON array of address line strings.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Tenant ID (GUID)")] string tenantId,
        [Description("Warehouse code (unique short identifier)")] string code,
        [Description("Display name as JSON: [{\"lang\":\"en\",\"name\":\"...\"}]")] string displayName,
        [Description("Ownership type (Own, ThreePl, Supplier, Dropship)")] string ownershipType,
        [Description("Timezone identifier (e.g. Europe/Moscow)")] string timezone,
        [Description("Operator counterparty ID (GUID, optional)")] string? operatorCounterpartyId = null,
        [Description("Address lines as JSON array of strings (optional)")] string? addressLines = null,
        [Description("City (optional)")] string? city = null,
        [Description("Region (optional)")] string? region = null,
        [Description("Country (optional)")] string? country = null,
        [Description("Postal code (optional)")] string? postalCode = null,
        [Description("Whether this warehouse is a public pickup point (default false)")] bool isPublicPickupPoint = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return McpProtoHelper.FormatError("tenantId is required");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return McpProtoHelper.FormatError("displayName is required");
        }

        var request = new CreateWarehouseRequest
        {
            TenantId = new GuidValue(tenantId),
            Code = code,
            OwnershipType = Enum.Parse<WarehouseOwnershipType>(ownershipType, ignoreCase: true),
            Timezone = timezone,
            IsPublicPickupPoint = isPublicPickupPoint,
        };

        // Parse displayName JSON [{lang, name}] into map
        try
        {
            var entries = JsonSerializer.Deserialize<List<JsonElement>>(displayName, McpJsonDefaults.Options);
            var localizedText = new WarehouseLocalizedText();
            if (entries is not null)
            {
                foreach (var entry in entries)
                {
                    var lang = entry.GetProperty("lang").GetString() ?? "";
                    var name = entry.GetProperty("name").GetString() ?? "";
                    localizedText.Values[lang] = name;
                }
            }

            request.DisplayName = localizedText;
        }
        catch (Exception)
        {
            return McpProtoHelper.FormatError("displayName JSON is invalid");
        }

        if (!string.IsNullOrWhiteSpace(operatorCounterpartyId))
        {
            request.OperatorCounterpartyId = new GuidValue(operatorCounterpartyId);
        }

        // Build address if any fields provided
        if (!string.IsNullOrWhiteSpace(addressLines) || !string.IsNullOrWhiteSpace(city)
            || !string.IsNullOrWhiteSpace(region) || !string.IsNullOrWhiteSpace(country)
            || !string.IsNullOrWhiteSpace(postalCode))
        {
            var address = new WarehouseAddress();

            if (!string.IsNullOrWhiteSpace(addressLines))
            {
                try
                {
                    var lines = JsonSerializer.Deserialize<List<string>>(addressLines, McpJsonDefaults.Options);
                    if (lines is not null)
                    {
                        address.Lines.AddRange(lines);
                    }
                }
                catch (Exception)
                {
                    return McpProtoHelper.FormatError("addressLines JSON is invalid");
                }
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                address.City = city;
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                address.Region = region;
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                address.Country = country;
            }

            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                address.PostalCode = postalCode;
            }

            request.Address = address;
        }

        var warehouse = await client.CreateWarehouseAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseTool.MapWarehouse(warehouse), McpJsonDefaults.Options);
    }
}
