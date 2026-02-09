using DKH.McpGateway.Application;
using DKH.OrderService.Contracts.Api.V1;
using DKH.Platform;
using DKH.Platform.Grpc.Client;
using DKH.Platform.Logging;
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
using DKH.ReferenceService.Contracts.Api.CountriesCrud.V1;
using DKH.ReferenceService.Contracts.Api.CountryQuery.V1;
using DKH.ReferenceService.Contracts.Api.CurrenciesCrud.V1;
using DKH.ReferenceService.Contracts.Api.CurrencyQuery.V1;
using DKH.ReferenceService.Contracts.Api.DeliveryQuery.V1;
using DKH.ReferenceService.Contracts.Api.LanguageQuery.V1;
using DKH.ReferenceService.Contracts.Api.LanguagesCrud.V1;
using DKH.ReferenceService.Contracts.Api.MeasurementQuery.V1;
using DKH.ReviewService.Contracts.Api.V1;
using DKH.StorefrontService.Contracts.V1;
using DKH.TelegramBotService.Contracts.Management.V1;
using DKH.TelegramBotService.Contracts.Scheduling.V1;
using GrpcReviewService = DKH.ReviewService.Contracts.Api.V1.ReviewService;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ReferenceDataExchangeClient =
    DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;

var useStdio = args.Contains("--stdio") ||
               Environment.GetEnvironmentVariable("MCP_TRANSPORT")?.Equals("stdio", StringComparison.OrdinalIgnoreCase) == true;

if (useStdio)
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services
        .AddMcpServer(options =>
            options.ServerInfo = new() { Name = "DKH.McpGateway", Version = "1.0.0" })
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(ConfigureServices).Assembly);

    var host = builder.Build();
    await host.RunAsync();
}
else
{
    await Platform
        .CreateWeb(args)
        .ConfigurePlatformWebApplicationBuilder(builder =>
        {
            builder.Services
                .AddMcpServer(options =>
                    options.ServerInfo = new() { Name = "DKH.McpGateway", Version = "1.0.0" })
                .WithHttpTransport()
                .WithToolsFromAssembly(typeof(ConfigureServices).Assembly);

            builder.Services.AddApplication();
        })
        .ConfigurePlatformWebApplication(app => app.MapMcp())
        .AddPlatformLogging()
        .AddPlatformGrpcEndpoints((_, grpc) =>
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
            grpc.AddEndpointFromConfiguration<ReferenceDataExchangeClient>("ReferenceDataExchangeServiceClient");

            // OrderService (5007) — analytics only
            grpc.AddEndpointFromConfiguration<OrderService.OrderServiceClient>();

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
        })
        .Build()
        .RunAsync();
}
