using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class GetStorefrontBrandingTool
{
    [McpServerTool(Name = "get_storefront_branding"), Description("Get storefront branding: logo, colors, typography, and layout.")]
    public static async Task<string> ExecuteAsync(
        StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient client,
        [Description("Storefront ID (UUID)")] string storefrontId,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetBrandingAsync(
            new GetBrandingRequest { StorefrontId = new GuidValue(storefrontId) },
            cancellationToken: cancellationToken);

        var b = response.Branding;
        var result = new
        {
            storefrontId = b.StorefrontId,
            logo = b.Logo,
            favicon = b.Favicon,
            ogImage = b.OgImage,
            customCss = string.IsNullOrEmpty(b.CustomCss) ? null : b.CustomCss,
            colors = b.Colors is not null
                ? new
                {
                    primary = b.Colors.Primary,
                    secondary = b.Colors.Secondary,
                    accent = b.Colors.Accent,
                    background = b.Colors.Background,
                    surface = b.Colors.Surface,
                    text = b.Colors.Text,
                    border = b.Colors.Border,
                }
                : null,
            typography = b.Typography is not null
                ? new
                {
                    fontFamily = b.Typography.FontFamily,
                    fontFamilyHeading = b.Typography.FontFamilyHeading,
                    baseFontSize = b.Typography.BaseFontSize,
                }
                : null,
            layout = b.Layout is not null
                ? new
                {
                    headerStyle = b.Layout.HeaderStyle,
                    productCardStyle = b.Layout.ProductCardStyle,
                    gridColumns = b.Layout.GridColumns,
                    borderRadius = b.Layout.BorderRadius,
                }
                : null,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
