# MCP Tools — справочник

> Translation Pending — canonical version: [en/mcp-tools.md](../en/mcp-tools.md)

## Обмен данными

`product_catalog_data` — как и workspace-скоупные инструменты чтения каталога
(`list_catalogs`, `list_categories`, `get_product`, `product_stats`,
`category_distribution`, `get_product_origin`) — доступен только через
аутентифицированный HTTP с ключом `ApiKeyScope.Mcp` и ровно одним заголовком
`X-Workspace-Id`; выбранный Workspace передаётся как gRPC metadata в каждом
downstream-вызове. Для `import` требуется
`mcp:write`; для `export`, `validate` и `template` — `mcp:read`. ProductCatalogService
независимо проверяет выбранный Workspace по переданной identity пользователя.
Поддерживаемая публичная tool-поверхность storefront-ключей — read-only пространство
`storefront_*`; такие ключи не могут вызывать массовый импорт или экспорт.

Storefront admin-инструменты, которые находят storefront по коду или ID, используют
тот же HTTP-only Workspace-контекст. До любого вызова branding, catalog, channel,
domain, feature, update или delete они сверяют принадлежащий StorefrontService
`WorkspaceId`. Чужой и отсутствующий storefront возвращают одинаковый нейтральный
`NotFound`, а cache `storefront://config` разделён по Workspace.
