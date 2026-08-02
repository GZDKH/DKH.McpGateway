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
`NotFound`.

Ресурсы `catalog://*` и `storefront://*` видны и читаются только через
аутентифицированный HTTP: нужен MCP-scoped API key с правом `mcp:read`,
аутентифицированный пользователь и ровно один `X-Workspace-Id`. Проверка
выполняется и при discovery, и повторно перед первым downstream-вызовом.
Storefront-ключи и stdio не могут перечислять или читать эти merchant-ресурсы.
Tenant-зависимые ответы в Gateway не кэшируются, поэтому данные не переиспользуются
между ключами, пользователями и Workspace. Общий cache остаётся только у глобальных
справочников `reference://*`.

## Версии справочников

Инструменты `manage_currency`, `manage_quantity_unit` и `manage_weight` выполняют
запись через канонические CRUD API ReferenceService. Действия `get` и `list`
возвращают стабильный `id` и нативный `authorityVersion`. Для `update` и `delete`
нужно передать именно прочитанный ID в `stableId` и точную положительную версию в
`expectedAuthorityVersion`. Цель изменения никогда не определяется повторно по
переиспользуемому человекочитаемому коду: удалённая и созданная заново позиция не
будет изменена случайно. Отсутствующие, некорректные, нулевые или устаревшие данные
identity отклоняются без автоматической перезаписи и без повторного запроса. После
конфликта нужно заново прочитать позицию и явно решить, следует ли повторять
изменение. Локализованные management API для этих трёх инструментов используются
только для чтения и поиска по коду.
