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

### Cart (4 tools)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `list_carts` | ListCartsTool.cs | List shopping carts with filtering and pagination |
| `get_cart` | GetCartTool.cs | Get a single cart by ID (customer PII omitted) |
| `issue_cart_claim_code` | IssueClaimCodeTool.cs | Issue an HMAC-signed claim code for phone-to-POS cart handoff |
| `claim_cart` | ClaimCartTool.cs | POS-side: claim a cart into a cashier session via a claim code |

### Payment (4 tools, read-only)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_payment` | GetPaymentTool.cs | Get a payment by ID — status, amounts, provider, refunds, history (PII omitted) |
| `list_payments` | ListPaymentsTool.cs | List payments filtered by storefront, order, status, date range |
| `get_payment_plan` | GetPaymentPlanTool.cs | Get a split payment plan with its schedule entries |
| `list_payment_plans` | ListPaymentPlansTool.cs | List payment plans filtered by storefront, order, status |

### Subscription (3 tools, read-only)

| Tool | File | Description |
| ---- | ---- | ----------- |
| `list_subscription_plans` | ListPlansTool.cs | List subscription plans (code, display names, price, features) |
| `get_user_subscription` | GetUserSubscriptionTool.cs | Get a user's current subscription by user ID |
| `list_user_subscriptions` | ListUserSubscriptionsTool.cs | List user subscriptions filtered by user, status, plan code |

### Customs (44 tools)

Customs tools expose the DKH.CustomsService declaration, duty, document-packet, trade-restriction, HS-code, and nomenclature-system surfaces. Responses omit generated document `content` bytes; `calculate_customs_duties` is kept as a backwards-compatible alias for `calculate_duties`.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `create_customs_declaration` | DeclarationTools.cs | Create a customs declaration with declared HS-code items |
| `update_customs_declaration_items` | DeclarationTools.cs | Replace declaration items |
| `attach_customs_certificate` | DeclarationTools.cs | Attach a certificate document reference to a declaration |
| `submit_customs_declaration` | DeclarationTools.cs | Submit a declaration with an optional filing reference |
| `update_customs_declaration_status` | DeclarationTools.cs | Advance or reject declaration status |
| `get_customs_declaration` | GetCustomsDeclarationTool.cs | Get a customs declaration by ID |
| `list_customs_declarations` | DeclarationTools.cs | List declarations with fulfillment, shipment, and status filters |
| `create_duty_rule` | DutyLookupTools.cs | Create a customs duty rule |
| `update_duty_rule` | DutyLookupTools.cs | Update mutable duty-rule fields |
| `expire_duty_rule` | DutyLookupTools.cs | Expire a customs duty rule |
| `get_duty_rule` | DutyLookupTools.cs | Get a customs duty rule by ID |
| `list_duty_rules` | DutyLookupTools.cs | List duty rules by destination, origin, system, prefix, and date |
| `calculate_duties` | DutyLookupTools.cs | Calculate customs duties for HS lines |
| `calculate_customs_duties` | DutyLookupTools.cs | Legacy alias for duty calculation |
| `create_trade_restriction` | TradeRestrictionTools.cs | Create a destination/origin/HS-prefix restriction |
| `check_trade_restriction` | TradeRestrictionTools.cs | Check whether a route and HS code are restricted |
| `create_document_packet` | DocumentPacketTools.cs | Create a customs document packet |
| `add_document_packet_item` | DocumentPacketTools.cs | Add a document reference to a packet |
| `generate_standard_customs_documents` | GenerateStandardCustomsDocumentsTool.cs | Generate standard customs document metadata without returning bytes |
| `compile_document_packet` | DocumentPacketTools.cs | Compile a customs document packet |
| `submit_document_packet` | DocumentPacketTools.cs | Submit a packet with an optional filing reference |
| `acknowledge_document_packet` | DocumentPacketTools.cs | Mark a submitted packet as acknowledged |
| `get_document_packet` | DocumentPacketTools.cs | Get a customs document packet by ID |
| `create_wco_hs_code` | HsCodeMutationTools.cs | Create a WCO HS code row |
| `update_wco_hs_code` | HsCodeMutationTools.cs | Update WCO HS code notes and translations |
| `retire_wco_hs_code` | HsCodeMutationTools.cs | Retire a WCO HS code |
| `reinstate_wco_hs_code` | HsCodeMutationTools.cs | Reinstate a retired WCO HS code |
| `get_wco_hs_code` | HsCodeLookupTools.cs | Get a WCO HS code by ID |
| `get_wco_hs_code_by_code` | HsCodeLookupTools.cs | Get a WCO HS code by code and revision |
| `list_wco_hs_codes` | HsCodeLookupTools.cs | List WCO HS codes by code prefix, level, revision, and status |
| `get_wco_hs_hierarchy` | HsCodeLookupTools.cs | Get a WCO HS hierarchy |
| `create_national_hs_code` | HsCodeMutationTools.cs | Create a national HS code row |
| `update_national_hs_code` | HsCodeMutationTools.cs | Update national HS code dates, notes, and translations |
| `retire_national_hs_code` | HsCodeMutationTools.cs | Retire a national HS code |
| `reinstate_national_hs_code` | HsCodeMutationTools.cs | Reinstate a retired national HS code |
| `get_national_hs_code` | HsCodeLookupTools.cs | Get a national HS code by ID |
| `get_national_hs_code_by_full_code` | HsCodeLookupTools.cs | Get a national HS code by system code and full code |
| `list_national_hs_codes` | HsCodeLookupTools.cs | List national HS codes by system, code prefix, level, date, and status |
| `get_national_hs_hierarchy` | HsCodeLookupTools.cs | Get a national HS hierarchy |
| `create_nomenclature_system` | NomenclatureSystemMutationTools.cs | Create a nomenclature system |
| `update_nomenclature_system` | NomenclatureSystemMutationTools.cs | Update a nomenclature system |
| `get_nomenclature_system` | NomenclatureSystemLookupTools.cs | Get a nomenclature system by ID |
| `get_nomenclature_system_by_code` | NomenclatureSystemLookupTools.cs | Get a nomenclature system by code |
| `list_nomenclature_systems` | NomenclatureSystemLookupTools.cs | List nomenclature systems by region |

### Counterparty (36 tools)

Counterparty tools expose the DKH.CounterpartyService identity, media/document, ACL, verification, partner-relationship, AP-balance, and financial-dashboard surfaces. Responses omit PII fields (`legalName`, `taxId`, `registrationNumber`, `email`, `phone`, `address`) and audit-log PII changes. Contact-channel RPCs are intentionally not exposed because contact `value` is PII.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_counterparty` | CounterpartyCrudTools.cs | Get a counterparty by ID with PII omitted |
| `list_counterparties` | CounterpartyCrudTools.cs | List counterparties with optional filters and PII omitted |
| `create_counterparty` | CounterpartyCrudTools.cs | Create a counterparty; accepts PII inputs but does not echo them |
| `update_counterparty` | CounterpartyCrudTools.cs | Update a counterparty; accepts PII inputs but does not echo them |
| `archive_counterparty` | CounterpartyCrudTools.cs | Archive a counterparty |
| `batch_get_counterparty_basics` | CounterpartyCrudTools.cs | Fetch lightweight non-PII basics for multiple counterparties |
| `set_counterparty_capabilities` | CounterpartyCrudTools.cs | Replace or clear logistics/service capabilities |
| `attach_counterparty_media` | CounterpartyCrudTools.cs | Attach a media reference |
| `detach_counterparty_media` | CounterpartyCrudTools.cs | Detach a media reference |
| `set_primary_counterparty_media` | CounterpartyCrudTools.cs | Promote a media row to primary |
| `list_counterparty_media` | CounterpartyCrudTools.cs | List media rows |
| `attach_counterparty_document` | CounterpartyCrudTools.cs | Attach a verification/compliance document |
| `detach_counterparty_document` | CounterpartyCrudTools.cs | Detach a document row |
| `verify_counterparty_document` | CounterpartyCrudTools.cs | Mark a document as verified |
| `reject_counterparty_document` | CounterpartyCrudTools.cs | Reject a document |
| `list_counterparty_documents` | CounterpartyCrudTools.cs | List documents for a counterparty |
| `list_expiring_documents` | CounterpartyCrudTools.cs | List documents expiring within a day window |
| `import_counterparty` | CounterpartyCrudTools.cs | Idempotently import a counterparty with caller-provided ID |
| `list_counterparty_audit_log` | CounterpartyCrudTools.cs | List audit entries with PII change keys filtered |
| `grant_counterparty_access` | CounterpartyCrudTools.cs | Grant or update a user's counterparty ACL |
| `revoke_counterparty_access` | CounterpartyCrudTools.cs | Revoke a user's counterparty ACL |
| `list_counterparty_acl` | CounterpartyCrudTools.cs | List ACL entries |
| `submit_for_verification` | CounterpartyCrudTools.cs | Submit a counterparty for verification |
| `approve_verification` | CounterpartyCrudTools.cs | Approve a verification attempt |
| `reject_verification` | CounterpartyCrudTools.cs | Reject a verification attempt |
| `list_verification_attempts` | CounterpartyCrudTools.cs | List verification attempts |
| `get_counterparty_business_relationship` | CounterpartyCrudTools.cs | Get the derived business relationship snapshot |
| `activate_partner_relationship` | PartnerRelationshipTools.cs | Activate a partner relationship |
| `suspend_partner_relationship` | PartnerRelationshipTools.cs | Suspend a partner relationship |
| `reactivate_partner_relationship` | PartnerRelationshipTools.cs | Reactivate a partner relationship |
| `terminate_partner_relationship` | PartnerRelationshipTools.cs | Terminate a partner relationship |
| `update_partner_relationship_terms` | PartnerRelationshipTools.cs | Update commercial relationship terms |
| `get_partner_relationship` | PartnerRelationshipTools.cs | Get a partner relationship by ID |
| `list_partner_relationships_by_counterparty` | PartnerRelationshipTools.cs | List partner relationship history for a counterparty |
| `get_counterparty_balance` | CounterpartyFinancialTools.cs | Get AP balance rows |
| `get_counterparty_financial_dashboard` | CounterpartyFinancialTools.cs | Get the financial dashboard projection |

### Staff (16 tools)

Staff tools expose the DKH.StaffService employee, department, onboarding, working-shift, cashier-shift, and device-presence surfaces. Employee responses omit personal identity fields (`fullName`, `email`, `keycloakUserId`). Device heartbeat ingestion is intentionally not exposed because it is internal telemetry.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_employee` | StaffTools.cs | Get an employee by ID with employee PII omitted |
| `list_employees` | StaffTools.cs | List employees with optional filters and PII omitted |
| `get_employee_by_keycloak_user_id` | StaffTools.cs | Look up an employee by Keycloak user ID without echoing the lookup key |
| `get_department` | StaffTools.cs | Get a department by ID |
| `list_departments` | StaffTools.cs | List departments |
| `get_onboarding_checklist` | StaffTools.cs | Get an onboarding checklist by ID |
| `get_employee_onboarding_checklist` | StaffTools.cs | Get an employee onboarding checklist |
| `open_working_shift` | StaffTools.cs | Open a working shift |
| `close_working_shift` | StaffTools.cs | Close a working shift |
| `get_current_working_shift` | StaffTools.cs | Get the current working shift for an employee and module |
| `list_active_working_shifts` | StaffTools.cs | List active working shifts |
| `open_cashier_shift` | StaffTools.cs | Open a cashier shift |
| `close_cashier_shift` | StaffTools.cs | Close a cashier shift |
| `get_current_cashier_shift` | StaffTools.cs | Get the current cashier shift for an employee |
| `list_active_cashier_shifts` | StaffTools.cs | List active cashier shifts |
| `list_device_presences` | StaffTools.cs | List device presence records |

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

*Last updated: July 2026*
