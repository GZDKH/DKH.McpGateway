using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.Warehouse;

[McpServerToolType]
public static class UpdateWarehouseTool
{
    [McpServerTool(Name = "update_warehouse"), Description(
        "Update an existing warehouse. " +
        "Only provided fields are updated. " +
        "displayName is a JSON array of {\"lang\":\"en\",\"name\":\"...\"} objects.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        WarehouseCrudService.WarehouseCrudServiceClient client,
        [Description("Warehouse ID (GUID)")] string id,
        [Description("Display name as JSON: [{\"lang\":\"en\",\"name\":\"...\"}] (optional)")] string? displayName = null,
        [Description("Ownership type (Own, ThreePl, Supplier, Dropship — optional)")] string? ownershipType = null,
        [Description("Status (Active, Suspended, Archived — optional)")] string? status = null,
        [Description("Timezone identifier (optional)")] string? timezone = null,
        [Description("Operator counterparty ID (GUID, optional)")] string? operatorCounterpartyId = null,
        [Description("Address lines as JSON array of strings (optional)")] string? addressLines = null,
        [Description("City (optional)")] string? city = null,
        [Description("Region (optional)")] string? region = null,
        [Description("Country (optional)")] string? country = null,
        [Description("Postal code (optional)")] string? postalCode = null,
        [Description("Whether this warehouse is a public pickup point (optional)")] bool? isPublicPickupPoint = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateWarehouseRequest
        {
            Id = new GuidValue(id),
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
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
        }

        if (!string.IsNullOrWhiteSpace(ownershipType))
        {
            request.OwnershipType = Enum.Parse<WarehouseOwnershipType>(ownershipType, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<WarehouseStatus>(status, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(timezone))
        {
            request.Timezone = timezone;
        }

        if (!string.IsNullOrWhiteSpace(operatorCounterpartyId))
        {
            request.OperatorCounterpartyId = new GuidValue(operatorCounterpartyId);
        }

        if (isPublicPickupPoint.HasValue)
        {
            request.IsPublicPickupPoint = isPublicPickupPoint.Value;
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

        var warehouse = await client.UpdateWarehouseAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetWarehouseTool.MapWarehouse(warehouse), McpJsonDefaults.Options);
    }
}
