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
| `manage_storefront` | ManageStorefrontTool.cs | Update or delete storefronts; legacy create returns a migration error until safe provisioning ships |
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

### TelegramClient (14 tools)

TelegramClient tools expose the DKH.TelegramClientService message archive, media, chat-monitoring, and read-only session surfaces. Session phone numbers and auth credentials are intentionally omitted; auth/session mutation RPCs are not exposed.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_messages` | GetMessagesTool.cs | Get archived Telegram messages for a monitored chat |
| `search_messages` | SearchMessagesTool.cs | Search archived Telegram messages in a monitored chat |
| `get_media` | GetMediaTool.cs | Get Telegram media attachment metadata |
| `download_media` | DownloadMediaTool.cs | Download Telegram media to service storage and return metadata |
| `export_messages` | ExportMessagesTool.cs | Export archived messages as base64 content with length and content type |
| `add_monitored_chat` | AddMonitoredChatTool.cs | Add a public Telegram chat to monitoring |
| `remove_monitored_chat` | RemoveMonitoredChatTool.cs | Remove a Telegram chat from monitoring |
| `pause_monitored_chat` | PauseMonitoredChatTool.cs | Pause Telegram chat monitoring |
| `resume_monitored_chat` | ResumeMonitoredChatTool.cs | Resume Telegram chat monitoring |
| `list_monitored_chats` | ListMonitoredChatsTool.cs | List Telegram chats monitored by a session |
| `get_monitoring_status` | GetMonitoringStatusTool.cs | Get Telegram chat monitoring and backfill status |
| `trigger_backfill` | TriggerBackfillTool.cs | Trigger Telegram chat history backfill |
| `get_telegram_session` | GetTelegramSessionTool.cs | Get a Telegram session with phone number omitted |
| `list_telegram_sessions` | ListTelegramSessionsTool.cs | List Telegram sessions with phone numbers omitted |

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

### Engagement (17 tools)

Engagement tools expose the DKH.EngagementService request lifecycle, template, and report surfaces. Responses omit requester/provider identity values (`keycloakUserId`, `sourceId`). ProfileService RPCs are intentionally not exposed because provider profiles are person-identity centric.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `create_service_request` | CreateServiceRequestTool.cs | Create an engagement service request without echoing requester identity |
| `assign_provider` | AssignProviderTool.cs | Assign a provider without echoing provider identity |
| `start_service_request` | StartServiceRequestTool.cs | Start an assigned engagement service request |
| `complete_service_request` | CompleteServiceRequestTool.cs | Complete an engagement service request |
| `cancel_service_request` | CancelServiceRequestTool.cs | Cancel an engagement service request with an optional reason |
| `get_service_request` | GetServiceRequestTool.cs | Get a service request with requester/provider identity omitted |
| `list_service_requests` | ListServiceRequestsTool.cs | List service requests with optional status filtering |
| `list_assigned_requests` | ListAssignedRequestsTool.cs | List requests assigned to a provider without echoing the lookup key |
| `create_service_template` | CreateServiceTemplateTool.cs | Create a service template with optional form schema JSON |
| `get_service_template` | GetServiceTemplateTool.cs | Get a service template by ID |
| `publish_service_template` | PublishServiceTemplateTool.cs | Publish a service template |
| `list_service_templates` | ListServiceTemplatesTool.cs | List service templates with optional type and published filters |
| `create_service_report` | CreateServiceReportTool.cs | Create a service report for a request/template version |
| `save_service_report_answers` | SaveServiceReportAnswersTool.cs | Save report answers from JSON input |
| `submit_service_report` | SubmitServiceReportTool.cs | Submit an engagement service report |
| `get_service_report` | GetServiceReportTool.cs | Get an engagement service report by ID |
| `review_service_report` | ReviewServiceReportTool.cs | Accept or reject an engagement service report |

### Print (5 tools)

Print tools expose the DKH.PrintService printer registry and print-job queue. No customer PII and no money movement; `payloadRef` is an opaque storage reference (not payload content) and `routingHints` is operational metadata.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `register_printer` | RegisterPrinterTool.cs | Register a printer (name, connection string, type Thermal/Label/Office, optional location) |
| `list_printers` | ListPrintersTool.cs | List printers, optionally filtered by location |
| `route_print_job` | RoutePrintJobTool.cs | Queue a print job (job type + opaque payload ref + routing hints) → Queued |
| `get_print_job` | GetPrintJobTool.cs | Get a print job by ID — type, payload ref, routing hints, lifecycle status, timestamps |
| `list_print_jobs` | ListPrintJobsTool.cs | List print jobs, filtered by status, with pagination |

### Assistant (4 tools)

Assistant tools expose the DKH.AssistantService conversational chat surface. `userId` is accepted only as an optional personalization input and is never echoed in tool responses. Assistant config and operator-chat RPCs are intentionally excluded from this thin onboarding slice.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `assistant_chat` | AssistantChatTool.cs | Send a chat message and return text, product cards, suggestions, intent, cache flag, and session count |
| `assistant_chat_stream` | AssistantChatStreamTool.cs | Send a streaming chat message and return one aggregated JSON response |
| `assistant_get_suggestions` | AssistantGetSuggestionsTool.cs | Get conversation-aware suggestions for a storefront session |
| `assistant_clear_session` | AssistantClearSessionTool.cs | Clear AssistantService conversation context for a storefront session |

### ProductRequest (12 tools)

ProductRequest tools expose the DKH.ProductRequestService request CRUD and status-transition surface. The service model contains no contact PII fields (no email, phone, or address); `customerId` is retained as an opaque identifier.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_product_request` | GetProductRequestTool.cs | Get a product request by ID |
| `list_product_requests` | ListProductRequestsTool.cs | List product requests by storefront with status/customer/soft-delete filters |
| `create_product_request` | CreateProductRequestTool.cs | Create a product request with optional category/source/price/quantity/photo URLs |
| `update_product_request` | UpdateProductRequestTool.cs | Update product request details |
| `delete_product_request` | DeleteProductRequestTool.cs | Soft-delete a product request |
| `restore_product_request` | RestoreProductRequestTool.cs | Restore a soft-deleted product request |
| `permanently_delete_product_request` | PermanentlyDeleteProductRequestTool.cs | Hard-delete a product request |
| `start_review_product_request` | StartReviewProductRequestTool.cs | Move a product request into review |
| `mark_found_product_request` | MarkFoundProductRequestTool.cs | Mark a product request as found and link a catalog product |
| `mark_not_found_product_request` | MarkNotFoundProductRequestTool.cs | Mark a product request as not found |
| `cancel_product_request` | CancelProductRequestTool.cs | Cancel a product request |
| `set_product_request_translation` | SetProductRequestTranslationTool.cs | Set localized product request fields |

### Broadcast (7 tools)

Broadcast tools expose the DKH.BroadcastService broadcast CRUD and schedule/cancel/retry surface. `targetConfig` describes audience/channel configuration and is retained; no email, phone, or name fields are present. `ReportDeliveryResult` is intentionally excluded because it is an internal delivery callback.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_broadcast` | GetBroadcastTool.cs | Get a broadcast by ID |
| `list_broadcasts` | ListBroadcastsTool.cs | List broadcasts with storefront, target type, status, and pagination filters |
| `create_broadcast` | CreateBroadcastTool.cs | Create and schedule a broadcast |
| `update_broadcast` | UpdateBroadcastTool.cs | Update broadcast content or schedule fields |
| `delete_broadcast` | DeleteBroadcastTool.cs | Delete a broadcast |
| `retry_broadcast` | RetryBroadcastTool.cs | Retry a failed broadcast |
| `cancel_broadcast` | CancelBroadcastTool.cs | Cancel a pending broadcast |

### Notification (5 tools, read-only)

Notification tools expose the DKH.NotificationService delivery health/status and bulk-job query surface. Sending, cancel, admin notification, user notification, and callback RPCs are intentionally not exposed. Delivery `recipient` values are omitted from all responses because they can contain email addresses, phone numbers, or chat IDs.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `get_notification_health` | GetNotificationHealthTool.cs | Get delivery health counters by status and channel |
| `get_notification_status` | GetNotificationStatusTool.cs | Get delivery status by order ID with recipient contact values omitted |
| `get_bulk_job_status` | GetBulkJobStatusTool.cs | Get bulk notification job status and counters |
| `list_bulk_jobs` | ListBulkJobsTool.cs | List bulk notification jobs with optional status filter and pagination |
| `get_bulk_job_failures` | GetBulkJobFailuresTool.cs | List failed bulk-job recipients with contact values omitted |

### Media (8 tools)

Media tools expose the DKH.MediaService asset, attachment, upload-session, and scope-registry surfaces. Responses omit internal actor identifiers (`attached_by_id`, `requested_by_id`); signed, time-limited upload/download URLs are returned by design.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `media_get_asset` | GetAssetTool.cs | Get a media asset's storage reference (container/key/type/size) by ID |
| `media_get_asset_download_link` | GetAssetDownloadLinkTool.cs | Get a signed, time-limited download link for an asset |
| `media_list_scopes` | ListScopesTool.cs | List registered media scopes and their constraints |
| `media_get_attachments` | GetAttachmentsTool.cs | List attachments for a scope/scope-key, optionally filtered by role |
| `media_update_attachment_metadata` | UpdateAttachmentMetadataTool.cs | Update an attachment's alt text and caption |
| `media_change_attachment_sort_order` | ChangeAttachmentSortOrderTool.cs | Reorder attachments within a scope/role |
| `media_detach` | DetachTool.cs | Detach (remove) an attachment |
| `media_create_upload_session` | CreateUploadSessionTool.cs | Create an upload session and return its signed upload URL |

### Data exchange (5 tools)

`product_catalog_data` — like the workspace-scoped catalog read tools
(`list_catalogs`, `list_categories`, `get_product`, `product_stats`,
`category_distribution`, `get_product_origin`) — is available only over
authenticated HTTP with an `ApiKeyScope.Mcp` key and exactly one
`X-Workspace-Id` header; the selected Workspace is propagated as gRPC
metadata on every downstream call. `import` requires
`mcp:write`; `export`, `validate`, and `template` require `mcp:read`. The
selected Workspace is independently checked by ProductCatalogService against
the propagated caller. The supported public tool surface for storefront-scoped
keys is the read-only `storefront_*` namespace; those keys cannot invoke bulk
data exchange.

| Tool | File | Description |
| ---- | ---- | ----------- |
| `product_catalog_data` | ProductCatalogDataTool.cs | Workspace-scoped import/export/validation/templates for product catalog data |
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
