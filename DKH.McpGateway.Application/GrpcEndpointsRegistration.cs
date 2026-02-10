using DKH.Platform.Grpc.Client;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandsCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogsCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoriesCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ManufacturersCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.PackagesCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductsCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductSearchQuery.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.TagsCrud.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantQuery.v1;
using DKH.ReferenceService.Contracts.Api.CityQuery.V1;
using DKH.ReferenceService.Contracts.Api.CountriesCrud.V1;
using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;
using DKH.ReferenceService.Contracts.Api.CurrenciesCrud.V1;
using DKH.ReferenceService.Contracts.Api.CurrencyQuery.V1;
using DKH.ReferenceService.Contracts.Api.DeliveryQuery.V1;
using DKH.ReferenceService.Contracts.Api.DeliveryTimesCrud.V1;
using DKH.ReferenceService.Contracts.Api.LanguageQuery.V1;
using DKH.ReferenceService.Contracts.Api.LanguagesCrud.V1;
using DKH.ReferenceService.Contracts.Api.MeasurementQuery.V1;
using DKH.ReferenceService.Contracts.Api.StateProvinceQuery.V1;
using DKH.ReviewService.Contracts.Api.V1;
using DKH.StorefrontService.Contracts.V1;
using DKH.TelegramBotService.Contracts.Auth.V1;
using DKH.TelegramBotService.Contracts.Management.V1;
using DKH.TelegramBotService.Contracts.Notifications.V1;
using DKH.TelegramBotService.Contracts.Scheduling.V1;
using GrpcOrderService = DKH.OrderService.Contracts.Api.V1.OrderService;
using GrpcReviewService = DKH.ReviewService.Contracts.Api.V1.ReviewService;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ReferenceDataExchangeClient =
    DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;

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
        grpc.AddEndpointFromConfiguration<ProductQueryService.ProductQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductSearchQueryService.ProductSearchQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<BrandQueryService.BrandQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<CatalogQueryService.CatalogQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<CategoryQueryService.CategoryQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<VariantQueryService.VariantQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<BrandsCrudService.BrandsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<CatalogsCrudService.CatalogsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<CategoriesCrudService.CategoriesCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductsCrudService.ProductsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<TagsCrudService.TagsCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<ManufacturersCrudService.ManufacturersCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<PackagesCrudService.PackagesCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<ProductCatalogDataExchangeClient>("ProductCatalogDataExchangeServiceClient");

        // ReferenceService (5004)
        grpc.AddEndpointFromConfiguration<CountryQueryService.CountryQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<CurrencyQueryService.CurrencyQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<LanguageQueryService.LanguageQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<MeasurementQueryService.MeasurementQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<DeliveryQueryService.DeliveryQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<CountriesCrudService.CountriesCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<CurrenciesCrudService.CurrenciesCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<LanguagesCrudService.LanguagesCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<StateProvinceQueryService.StateProvinceQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<CityQueryService.CityQueryServiceClient>();
        grpc.AddEndpointFromConfiguration<DeliveryTimesCrudService.DeliveryTimesCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<ReferenceDataExchangeClient>("ReferenceDataExchangeServiceClient");

        // OrderService (5007)
        grpc.AddEndpointFromConfiguration<GrpcOrderService.OrderServiceClient>();

        // StorefrontService (5009)
        grpc.AddEndpointFromConfiguration<StorefrontCrudService.StorefrontCrudServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontBrandingService.StorefrontBrandingServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontCatalogService.StorefrontCatalogServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontChannelService.StorefrontChannelServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontDomainService.StorefrontDomainServiceClient>();
        grpc.AddEndpointFromConfiguration<StorefrontFeaturesService.StorefrontFeaturesServiceClient>();

        // ReviewService (5011)
        grpc.AddEndpointFromConfiguration<GrpcReviewService.ReviewServiceClient>();
        grpc.AddEndpointFromConfiguration<ReviewQueryService.ReviewQueryServiceClient>();

        // TelegramBotService (5001)
        grpc.AddEndpointFromConfiguration<TelegramBotManagement.TelegramBotManagementClient>();
        grpc.AddEndpointFromConfiguration<TelegramScheduling.TelegramSchedulingClient>();
        grpc.AddEndpointFromConfiguration<TelegramNotifications.TelegramNotificationsClient>();
        grpc.AddEndpointFromConfiguration<TelegramAuthService.TelegramAuthServiceClient>();
    }
}
