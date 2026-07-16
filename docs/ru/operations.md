# Эксплуатация

> Translation Pending — canonical version: [en/operations.md](../en/operations.md)

## Требования ProductCatalog data exchange

Обмен данными ProductCatalog доступен только через HTTP. MCP-клиент должен передать
MCP-scoped API key, аутентифицированную Keycloak-сессию и ровно один заголовок
`X-Workspace-Id`. Отсутствующие, пустые, невалидные или повторяющиеся значения
Workspace отклоняются до downstream-вызова. Stdio и глобальное выполнение без явно
выбранного Workspace намеренно работают в fail-closed режиме.
