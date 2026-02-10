# MCP tools reference

Complete reference of all MCP capabilities exposed by DKH.McpGateway.

## Tools

### Products (11 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `search_products` | SearchProductsTool.cs | Search products by query with pagination |
| `get_product` | GetProductTool.cs | Get detailed product information by SEO name |
| `list_brands` | ListBrandsTool.cs | List all available brands |
| `list_categories` | ListCategoriesTool.cs | List category tree for a catalog |
| `list_catalogs` | ListCatalogsTool.cs | List all product catalogs |
| `get_product_stats` | ProductStatsTool.cs | Product catalog statistics |
| `get_category_distribution` | CategoryDistributionTool.cs | Category product distribution analysis |
| `get_brand_analytics` | BrandAnalyticsTool.cs | Brand analytics and statistics |
| `create_product` | CreateProductTool.cs | Create a new product |
| `update_product` | UpdateProductTool.cs | Update an existing product |
| `delete_product` | DeleteProductTool.cs | Delete a product by code |

### Brands (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_brand` | ManageBrandTool.cs | Create, update, or delete brands (action parameter) |

### Categories (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_category` | ManageCategoryTool.cs | Create, update, or delete categories (action parameter) |

### Tags (1 tool)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `manage_tags` | ManageTagsTool.cs | Create, update, or delete tags (action parameter) |

### References (6 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `list_measurements` | ListMeasurementsTool.cs | List weight and dimension units |
| `list_delivery_times` | ListDeliveryTimesTool.cs | List delivery time options |
| `manage_country` | ManageCountryTool.cs | Create, update, or delete countries |
| `manage_currency` | ManageCurrencyTool.cs | Create, update, or delete currencies |
| `manage_language` | ManageLanguageTool.cs | Create, update, or delete languages |
| `manage_delivery_time` | ManageDeliveryTimeTool.cs | Create, update, or delete delivery times |

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

### Storefronts (12 tools)

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

### Data exchange (2 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `product_catalog_data` | ProductCatalogDataTool.cs | Import/export product catalog data |
| `reference_data` | ReferenceDataTool.cs | Import/export reference data |

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
