# MCP tools reference

Complete reference of all MCP capabilities exposed by DKH.McpGateway.

## Tools

### Products (9 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `search_products` | SearchProductsTool.cs | Search products by query with pagination |
| `get_product` | GetProductTool.cs | Get detailed product information by SEO name |
| `manage_product` | ManageProductTool.cs | Create, update, delete, get, or list products (action parameter) |
| `list_brands` | ListBrandsTool.cs | List all available brands |
| `list_categories` | ListCategoriesTool.cs | List category tree for a catalog |
| `list_catalogs` | ListCatalogsTool.cs | List all product catalogs |
| `get_product_stats` | ProductStatsTool.cs | Product catalog statistics |
| `get_category_distribution` | CategoryDistributionTool.cs | Category product distribution analysis |
| `get_brand_analytics` | BrandAnalyticsTool.cs | Brand analytics and statistics |

### Brands (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_brand` | ManageBrandTool.cs | Create, update, or delete brands (action parameter) |

### Catalogs (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_catalog` | ManageCatalogTool.cs | Create, update, delete, get, or list catalogs (action parameter) |

### Categories (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_category` | ManageCategoryTool.cs | Create, update, or delete categories (action parameter) |

### Tags (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_tags` | ManageTagsTool.cs | Create, update, or delete tags (action parameter) |

### Manufacturers (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_manufacturer` | ManageManufacturerTool.cs | Create, update, delete, get, or list manufacturers (action parameter) |

### Packages (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_package` | ManagePackageTool.cs | Create, update, delete, get, or list packages (action parameter) |

### Specifications (3 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_spec_group` | ManageSpecGroupTool.cs | Create, update, delete, get, or list specification groups (action parameter) |
| `manage_spec_attribute` | ManageSpecAttributeTool.cs | Create, update, delete, get, or list specification attributes (action parameter) |
| `manage_spec_option` | ManageSpecOptionTool.cs | Create, update, delete, get, or list specification options (action parameter) |

### Product attributes (3 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_product_attr_group` | ManageProductAttrGroupTool.cs | Create, update, delete, get, or list product attribute groups (action parameter) |
| `manage_product_attr` | ManageProductAttrTool.cs | Create, update, delete, get, or list product attributes (action parameter) |
| `manage_product_attr_option` | ManageProductAttrOptionTool.cs | Create, update, delete, get, or list product attribute options (action parameter) |

### Variants (2 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_variant_attr` | ManageVariantAttrTool.cs | Create, update, delete, get, or list variant attributes (action parameter) |
| `manage_variant_attr_value` | ManageVariantAttrValueTool.cs | Create, update, delete, get, or list variant attribute values (action parameter) |

### References (13 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `list_measurements` | ListMeasurementsTool.cs | List weight and dimension units |
| `list_delivery_times` | ListDeliveryTimesTool.cs | List delivery time options |
| `manage_country` | ManageCountryTool.cs | Create, update, or delete countries |
| `manage_currency` | ManageCurrencyTool.cs | Create, update, or delete currencies |
| `manage_language` | ManageLanguageTool.cs | Create, update, or delete languages |
| `manage_delivery_time` | ManageDeliveryTimeTool.cs | Create, update, or delete delivery times |
| `manage_city` | ManageCityTool.cs | Create, update, delete, get, or list cities (action parameter) |
| `manage_dimension` | ManageDimensionTool.cs | Create, update, delete, get, or list dimension units (action parameter) |
| `manage_price_label` | ManagePriceLabelTool.cs | Create, update, delete, get, or list price labels (action parameter) |
| `manage_quantity_unit` | ManageQuantityUnitTool.cs | Create, update, delete, get, or list quantity units (action parameter) |
| `manage_state_province` | ManageStateProvinceTool.cs | Create, update, delete, get, or list states/provinces (action parameter) |
| `manage_state_province_type` | ManageStateProvinceTypeTool.cs | Create, update, delete, get, or list state/province types (action parameter) |
| `manage_weight` | ManageWeightTool.cs | Create, update, delete, get, or list weight units (action parameter) |

### Geography (2 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_country_details` | CountryDetailsTool.cs | Get detailed country information by ISO code |
| `get_product_origin` | ProductOriginTool.cs | Get product origin country information |

### Orders (4 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_order_summary` | OrderSummaryTool.cs | Order summary and aggregated metrics |
| `get_order_status_distribution` | OrderStatusDistributionTool.cs | Order count by status |
| `get_order_trends` | OrderTrendsTool.cs | Order trends over time period |
| `get_top_selling_products` | TopSellingProductsTool.cs | Top selling products by order count/revenue |

### Reviews (3 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_review_stats` | ReviewStatsTool.cs | Review statistics and average ratings |
| `get_product_review_ranking` | ProductReviewRankingTool.cs | Products ranked by review score |
| `get_review_summary` | ReviewSummaryTool.cs | Review sentiment summary |

### Storefronts (11 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `list_storefronts` | ListStorefrontsTool.cs | List all storefronts |
| `get_storefront` | GetStorefrontTool.cs | Get storefront details by code |
| `get_storefront_branding` | GetStorefrontBrandingTool.cs | Get storefront branding (logo, colors) |
| `get_storefront_features` | GetStorefrontFeaturesTool.cs | Get storefront feature flags |
| `get_storefront_overview` | StorefrontOverviewTool.cs | Combined storefront overview (branding + features) |
| `manage_storefront` | ManageStorefrontTool.cs | Create, update, or delete storefronts |
| `manage_storefront_branding` | ManageStorefrontBrandingTool.cs | Update storefront branding |
| `manage_storefront_catalogs` | ManageStorefrontCatalogsTool.cs | Assign/remove catalogs from storefront |
| `manage_storefront_channels` | ManageStorefrontChannelsTool.cs | Manage storefront sales channels |
| `manage_storefront_domains` | ManageStorefrontDomainsTool.cs | Manage storefront domains |
| `manage_storefront_features` | ManageStorefrontFeaturesTool.cs | Toggle storefront feature flags |

### Telegram (4 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_telegram_bot` | ManageTelegramBotTool.cs | Create, update, or delete Telegram bots |
| `manage_telegram_channels` | ManageTelegramChannelsTool.cs | Manage Telegram channels |
| `manage_telegram_manager_groups` | ManageTelegramManagerGroupsTool.cs | Manage Telegram manager groups |
| `manage_telegram_scheduling` | ManageTelegramSchedulingTool.cs | Manage Telegram message scheduling |

### Inventory (4 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `query_stock` | QueryStockTool.cs | Query stock levels and check availability (action parameter) |
| `manage_stock` | ManageStockTool.cs | Set, adjust, or get warehouse stock levels (action parameter) |
| `manage_reservation` | ManageReservationTool.cs | Reserve, release, confirm, get, or list stock reservations (action parameter) |
| `manage_stock_alert` | ManageStockAlertTool.cs | List, configure, or acknowledge low stock alerts (action parameter) |

### Data exchange (5 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `product_catalog_data` | ProductCatalogDataTool.cs | Import/export product catalog data |
| `reference_data` | ReferenceDataTool.cs | Import/export reference data |
| `order_data` | OrderDataTool.cs | Import/export order data |
| `customer_data` | CustomerDataTool.cs | Import/export customer data |
| `review_data` | ReviewDataTool.cs | Import/export review data |

## Resources

Read-only data endpoints that AI clients can access directly.

| URI | Resource class | Description |
| --- | -------------- | ----------- |
| `catalog://catalogs` | CatalogResources | All product catalogs |
| `catalog://categories` | CatalogResources | Category tree (parameterized by catalog) |
| `catalog://products` | CatalogResources | Product details (parameterized by SEO name) |
| `reference://countries` | ReferenceResources | All countries with ISO codes |
| `reference://countries/details` | ReferenceResources | Country details by code |
| `reference://currencies` | ReferenceResources | All currencies with codes and symbols |
| `reference://languages` | ReferenceResources | All supported languages |
| `storefront://storefronts` | StorefrontResources | All storefronts |
| `storefront://config` | StorefrontResources | Storefront config with branding and features |

## Prompts

Analytics prompt templates that guide AI through multi-step analysis workflows.

| Prompt | File | Description |
| ------ | ---- | ----------- |
| `analyze_catalog` | AnalyzeCatalogPrompt.cs | Catalog health analysis and recommendations |
| `sales_report` | SalesReportPrompt.cs | Sales summary for a time period |
| `storefront_audit` | StorefrontAuditPrompt.cs | Storefront configuration audit |
| `review_analysis` | ReviewAnalysisPrompt.cs | Review sentiment and trends analysis |
| `data_quality_check` | DataQualityCheckPrompt.cs | Data completeness and quality check |

*Last updated: February 2026*
