# Архитектура

> Translation Pending — canonical version: [en/architecture.md](../en/architecture.md)

## Аутентификация и граница доверия

Административные MCP-инструменты с permission guard требуют ключ с
`ApiKeyScope.Mcp` и соответствующим разрешением `mcp:read` или `mcp:write`. Ключ со
scope `Storefront` не может вызывать эти инструменты, даже если ему по ошибке выдали
административное разрешение. Поддерживаемая публичная read-only tool-поверхность для
storefront-ключей остаётся пространством `storefront_*` из ADR-060.

HTTP host публикует path-aware RFC 9728 metadata для нативных OAuth-клиентов.
`Mcp:PublicEndpoint` фиксирует один resource и один absolute metadata URL для
Streamable HTTP и сохранённых legacy SSE routes. Штатный MCP SDK отклоняет
metadata-запрос, если scheme или host не совпадают с настроенным endpoint;
доверенные forwarded headers восстанавливают публичные значения на edge.
Документ содержит внешний Keycloak issuer, audience-mapping scope `mcp:tools` и
header-based передачу bearer token. Этот scope просит Keycloak добавить
канонический audience; доступ по-прежнему требует realm role и независимых
API-key scope/permissions. Metadata является единственным OAuth-specific обходом
проверки API-ключа; health и metrics остаются отдельными служебными endpoint-ами.
Ответы 401 для MCP содержат ссылку `resource_metadata`.

Обмен данными ProductCatalog — как и все workspace-скоупные admin/query
инструменты каталога (`list_catalogs`, `list_categories`, `get_product`,
`product_stats`, `category_distribution`, `get_product_origin`) — является
аутентифицированной merchant-поверхностью.
Каждый HTTP-вызов требует ровно один заголовок `X-Workspace-Id`. Gateway создаёт
новый gRPC metadata только с нормализованным `x-workspace-id`, не копируя произвольные
metadata клиента. ProductCatalogService независимо сверяет Workspace с переданной
Keycloak identity и активным membership пользователя.

Storefront admin-инструменты используют тот же выбранный Workspace-контекст.
Gateway проверяет `WorkspaceId`, возвращённый StorefrontService, до любого зависимого
storefront RPC, отображает чужой и отсутствующий ресурс в один нейтральный
`NotFound`.

Общие merchant-ресурсы `catalog://*` и `storefront://*` защищены
authorization-фильтрами MCP SDK как при discovery, так и при прямом чтении.
Нужны MCP-scoped ключ, право `mcp:read`, аутентифицированный HTTP principal и один
выбранный Workspace. Перед первым RPC каждый ресурс повторяет проверку, передаёт
только нормализованный Workspace metadata и не использует Gateway-cache для
tenant-зависимого ответа. Список storefront дополнительно запрашивается с
`OwnerId` выбранного Workspace, а каждый возвращённый `WorkspaceId` сверяется.
Storefront-ключи и stdio не видят эту merchant-поверхность.

В режиме stdio нет HTTP principal и передачи identity, поэтому ProductCatalog data
exchange и workspace-скоупные admin/query инструменты отклоняются. Fallback для отсутствующего заголовка, глобального ключа или
неявного trusted-system режима не предусмотрен.
