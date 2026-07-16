# MCP Tools — справочник

> Translation Pending — canonical version: [en/mcp-tools.md](../en/mcp-tools.md)

## Обмен данными

`product_catalog_data` доступен только через аутентифицированный HTTP с ключом
`ApiKeyScope.Mcp` и ровно одним заголовком `X-Workspace-Id`. Для `import` требуется
`mcp:write`; для `export`, `validate` и `template` — `mcp:read`. ProductCatalogService
независимо проверяет выбранный Workspace по переданной identity пользователя.
Поддерживаемая публичная tool-поверхность storefront-ключей — read-only пространство
`storefront_*`; такие ключи не могут вызывать массовый импорт или экспорт.
