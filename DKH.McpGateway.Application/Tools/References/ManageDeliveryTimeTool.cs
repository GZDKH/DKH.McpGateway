using DKH.ReferenceService.Contracts.Api.DeliveryTimesCrud.V1;
using DKH.ReferenceService.Contracts.Models.DeliveryTime.V1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageDeliveryTimeTool
{
    [McpServerTool(Name = "manage_delivery_time"), Description(
        "Create, update, or delete a delivery time. " +
        "For create: provide code, delivery day ranges, and translations. " +
        "For update/delete: provide code to identify the delivery time.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeliveryTimesCrudService.DeliveryTimesCrudServiceClient client,
        [Description("Action: create, update, or delete")] string action,
        [Description("Delivery time code, e.g. 'standard', 'express', 'next-day' (required)")] string code,
        [Description("Minimum delivery days (for create/update)")] int? minDays = null,
        [Description("Maximum delivery days (for create/update)")] int? maxDays = null,
        [Description("Color hex code, e.g. '#28a745' (for create/update)")] string? color = null,
        [Description("Is default delivery time (for create/update)")] bool? isDefault = null,
        [Description("Translations as JSON array: [{\"lang\":\"en\",\"name\":\"Standard delivery\"},{\"lang\":\"ru\",\"name\":\"Стандартная доставка\"}]")] string? translations = null,
        [Description("Display order (for create/update)")] int? displayOrder = null,
        [Description("Published (for create/update)")] bool? published = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            var request = new CreateDeliveryTimeRequest
            {
                Code = code,
                DeliveryNotBeforeDays = minDays ?? 1,
                DeliveryNotLateDays = maxDays ?? 5,
                Color = color ?? "",
                IsDefault = isDefault ?? false,
            };

            if (displayOrder.HasValue)
            {
                request.DisplayOrder = displayOrder.Value;
            }

            if (published.HasValue)
            {
                request.Published = published.Value;
            }

            AddTranslations(request.Translations, translations);

            var response = await client.CreateDeliveryTimeAsync(request, cancellationToken: cancellationToken);
            var d = response.DeliveryTime;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                deliveryTime = new
                {
                    d.Id,
                    d.Code,
                    d.DeliveryNotBeforeDays,
                    d.DeliveryNotLateDays,
                    d.IsDefault,
                },
            }, McpJsonDefaults.Options);
        }

        var found = await FindByCodeAsync(client, code, cancellationToken);
        if (found is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Delivery time with code '{code}' not found" },
                McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var request = new UpdateDeliveryTimeRequest
            {
                Id = found.Id,
                Code = code,
                DeliveryNotBeforeDays = minDays ?? found.DeliveryNotBeforeDays,
                DeliveryNotLateDays = maxDays ?? found.DeliveryNotLateDays,
                Color = color ?? found.Color,
                IsDefault = isDefault ?? found.IsDefault,
            };

            if (displayOrder.HasValue)
            {
                request.DisplayOrder = displayOrder.Value;
            }

            if (published.HasValue)
            {
                request.Published = published.Value;
            }

            if (!string.IsNullOrEmpty(translations))
            {
                AddTranslations(request.Translations, translations);
            }
            else
            {
                request.Translations.AddRange(found.Translations);
            }

            var response = await client.UpdateDeliveryTimeAsync(request, cancellationToken: cancellationToken);
            var d = response.DeliveryTime;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                deliveryTime = new
                {
                    d.Id,
                    d.Code,
                    d.DeliveryNotBeforeDays,
                    d.DeliveryNotLateDays,
                    d.IsDefault,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            await client.DeleteDeliveryTimeAsync(
                new DeleteDeliveryTimeRequest { Id = found.Id },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedCode = code,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }

    private static async Task<DeliveryTime?> FindByCodeAsync(
        DeliveryTimesCrudService.DeliveryTimesCrudServiceClient client, string code, CancellationToken ct)
    {
        var response = await client.GetDeliveryTimesAsync(
            new GetDeliveryTimesRequest { Filter = $"Code == \"{code}\"", PageSize = 1 },
            cancellationToken: ct);

        return response.DeliveryTimes.FirstOrDefault();
    }

    private static void AddTranslations(
        Google.Protobuf.Collections.RepeatedField<DeliveryTimeTranslation> field, string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        var items = JsonSerializer.Deserialize<List<TranslationInput>>(json, McpJsonDefaults.Options);
        if (items is null)
        {
            return;
        }

        foreach (var item in items)
        {
            field.Add(new DeliveryTimeTranslation
            {
                LanguageCode = item.Lang ?? "",
                Name = item.Name ?? "",
            });
        }
    }

    private sealed class TranslationInput
    {
        public string? Lang { get; set; }
        public string? Name { get; set; }
    }
}
