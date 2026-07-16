# Архитектура

> Translation Pending — canonical version: [en/architecture.md](../en/architecture.md)

## Аутентификация и граница доверия

Административные MCP-инструменты с permission guard требуют ключ с
`ApiKeyScope.Mcp` и соответствующим разрешением `mcp:read` или `mcp:write`. Ключ со
scope `Storefront` не может вызывать эти инструменты, даже если ему по ошибке выдали
административное разрешение. Поддерживаемая публичная read-only tool-поверхность для
storefront-ключей остаётся пространством `storefront_*` из ADR-060.

Обмен данными ProductCatalog является аутентифицированной merchant-поверхностью.
Каждый HTTP-вызов требует ровно один заголовок `X-Workspace-Id`. Gateway создаёт
новый gRPC metadata только с нормализованным `x-workspace-id`, не копируя произвольные
metadata клиента. ProductCatalogService независимо сверяет Workspace с переданной
Keycloak identity и активным membership пользователя.

В режиме stdio нет HTTP principal и передачи identity, поэтому ProductCatalog data
exchange отклоняется. Fallback для отсутствующего заголовка, глобального ключа или
неявного trusted-system режима не предусмотрен.
