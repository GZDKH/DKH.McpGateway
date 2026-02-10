using DKH.StorefrontService.Contracts.V1;
using DKH.StorefrontService.Contracts.V1.Models;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontBrandingTool
{
    [McpServerTool(Name = "manage_storefront_branding"), Description(
        "Update storefront branding: logo, favicon, theme colors, typography, layout, and custom CSS. " +
        "Use action 'update' to set branding, 'reset' to reset to defaults, 'get' to view current branding.")]
    public static async Task<string> ExecuteAsync(
        StorefrontCrudService.StorefrontCrudServiceClient crudClient,
        StorefrontBrandingService.StorefrontBrandingServiceClient brandingClient,
        [Description("Storefront code (e.g. 'my-store')")] string storefrontCode,
        [Description("Action: get, update, or reset")] string action,
        [Description("Logo URL")] string? logo = null,
        [Description("Favicon URL")] string? favicon = null,
        [Description("Primary color (hex, e.g. '#2563eb')")] string? primaryColor = null,
        [Description("Secondary color (hex)")] string? secondaryColor = null,
        [Description("Accent color (hex)")] string? accentColor = null,
        [Description("Background color (hex)")] string? backgroundColor = null,
        [Description("Text color (hex)")] string? textColor = null,
        [Description("Font family (e.g. 'Inter, sans-serif')")] string? fontFamily = null,
        [Description("Header style (e.g. 'sticky', 'static')")] string? headerStyle = null,
        [Description("Product card style (e.g. 'grid', 'list')")] string? productCardStyle = null,
        [Description("Grid columns count")] int? gridColumns = null,
        [Description("Custom CSS")] string? customCss = null,
        CancellationToken cancellationToken = default)
    {
        var storefront = await crudClient.GetByCodeAsync(
            new GetStorefrontByCodeRequest { Code = storefrontCode },
            cancellationToken: cancellationToken);

        if (storefront.Storefront is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Storefront '{storefrontCode}' not found" },
                McpJsonDefaults.Options);
        }

        var storefrontId = storefront.Storefront.Id;

        if (string.Equals(action, "get", StringComparison.OrdinalIgnoreCase))
        {
            var branding = await brandingClient.GetBrandingAsync(
                new GetBrandingRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                branding = branding.Branding,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "reset", StringComparison.OrdinalIgnoreCase))
        {
            var response = await brandingClient.ResetToDefaultAsync(
                new ResetBrandingRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "reset",
                branding = response.Branding,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var request = new UpdateBrandingRequest
            {
                StorefrontId = storefrontId,
                Colors = new ThemeColors
                {
                    Primary = primaryColor ?? "#2563eb",
                    Secondary = secondaryColor ?? "#64748b",
                    Accent = accentColor ?? "#f59e0b",
                    Background = backgroundColor ?? "#ffffff",
                    Text = textColor ?? "#0f172a",
                },
                Typography = new ThemeTypography
                {
                    FontFamily = fontFamily ?? "Inter, sans-serif",
                    BaseFontSize = 16,
                },
                Layout = new ThemeLayout
                {
                    HeaderStyle = headerStyle ?? "sticky",
                    ProductCardStyle = productCardStyle ?? "grid",
                    GridColumns = gridColumns ?? 3,
                    BorderRadius = "0.5rem",
                },
            };

            if (logo is not null)
            {
                request.Logo = logo;
            }

            if (favicon is not null)
            {
                request.Favicon = favicon;
            }

            if (customCss is not null)
            {
                request.CustomCss = customCss;
            }

            var response = await brandingClient.UpdateBrandingAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                branding = response.Branding,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: get, update, or reset" },
            McpJsonDefaults.Options);
    }
}
