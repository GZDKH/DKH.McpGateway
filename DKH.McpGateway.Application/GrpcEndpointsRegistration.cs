using DKH.ApiManagementService.Contracts.ApiManagement.Api.ApiKeyQuery.v1;
using DKH.ApiManagementService.Contracts.ApiManagement.Api.ApiKeyUsage.v1;
using DKH.OrderService.Contracts.Order.Api.OrderCrud.v1;
using DKH.Platform.Grpc.Client;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ManufacturerManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.PackageManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrGroupManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrOptionManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecAttributeManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecGroupManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecOptionManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.TagManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantAttrManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantAttrValueManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantQuery.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CityManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CountryManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.CurrencyManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.DeliveryTimeManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.DimensionManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.LanguageManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.PriceLabelManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.QuantityUnitManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.StateProvinceManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.StateProvinceTypeManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.WeightManagement.v1;
using DKH.ReviewService.Contracts.Review.Api.ReviewCrud.v1;
using DKH.ReviewService.Contracts.Review.Api.ReviewQuery.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontChannelManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontDomainManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotAuth.v1;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotCrud.v1;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotNotification.v1;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BroadcastManagement.v1;
using CustomerDataExchangeClient =
    DKH.CustomerService.Contracts.Customer.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using OrderDataExchangeClient =
    DKH.OrderService.Contracts.Order.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ReferenceDataExchangeClient =
    DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ReviewDataExchangeClient =
    DKH.ReviewService.Contracts.Review.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;

namespace DKH.McpGateway.Application;

/// <summary>
/// Centralized gRPC endpoint registration for all downstream services.
/// </summary>
public static class GrpcEndpointsRegistration
{
    /// <summary>
    /// Registers all MCP Gateway gRPC client endpoints from configuration.
    /// </summary>
    public static void AddMcpGatewayEndpoints(this IPlatformGrpcClientBuilder grpc)
    {
        // ProductCatalogService (5003)
        grpc.AddEndpointFromConfiguration<VariantQueryService.VariantQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductCatalogDataExchangeClient>("ProductCatalogDataExchangeServiceClient");
        grpc.AddEndpointFromConfiguration<BrandManagementService.BrandManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<CatalogManagementService.CatalogManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<CategoryManagementService.CategoryManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<ManufacturerManagementService.ManufacturerManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<PackageManagementService.PackageManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductManagementService.ProductManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<TagManagementService.TagManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<SpecGroupManagementService.SpecGroupManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<SpecAttributeManagementService.SpecAttributeManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<SpecOptionManagementService.SpecOptionManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductAttrManagementService.ProductAttrManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductAttrOptionManagementService.ProductAttrOptionManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<VariantAttrManagementService.VariantAttrManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<VariantAttrValueManagementService.VariantAttrValueManagementServiceClient>();

        // ReferenceService — Management (5004)
        grpc.AddEndpointFromConfiguration<CountryManagementService.CountryManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<CurrencyManagementService.CurrencyManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<LanguageManagementService.LanguageManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<DeliveryTimeManagementService.DeliveryTimeManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<DimensionManagementService.DimensionManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<WeightManagementService.WeightManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<QuantityUnitManagementService.QuantityUnitManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<PriceLabelManagementService.PriceLabelManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<StateProvinceManagementService.StateProvinceManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<StateProvinceTypeManagementService.StateProvinceTypeManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<CityManagementService.CityManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<ReferenceDataExchangeClient>("ReferenceDataExchangeServiceClient");

        // OrderService (5007)
        grpc.AddEndpointFromConfiguration<OrderCrudService.OrderCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<OrderDataExchangeClient>("OrderDataExchangeServiceClient");

        // CustomerService (5010)
        grpc.AddEndpointFromConfiguration<CustomerDataExchangeClient>("CustomerDataExchangeServiceClient");

        // StorefrontService (5009)
        grpc.AddEndpointFromConfiguration<StorefrontsCrudService.StorefrontsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontChannelManagementService.StorefrontChannelManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontDomainManagementService.StorefrontDomainManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient>();

        // ReviewService (5011)
        grpc.AddEndpointFromConfiguration<ReviewsCrudService.ReviewsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<ReviewQueryService.ReviewQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<ReviewDataExchangeClient>("ReviewDataExchangeServiceClient");

        // TelegramBotService (5001)
        grpc.AddEndpointFromConfiguration<BotsCrudService.BotsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<BroadcastManagementService.BroadcastManagementServiceClient>();
        grpc.AddEndpointFromConfiguration<BotNotificationService.BotNotificationServiceClient>();
        grpc.AddEndpointFromConfiguration<BotAuthService.BotAuthServiceClient>();

        // ApiManagementService (5012)
        grpc.AddEndpointFromConfiguration<ApiKeyQueryService.ApiKeyQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<ApiKeyUsageService.ApiKeyUsageServiceClient>();
    }
}
