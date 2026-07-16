# Эксплуатация

> Translation Pending — canonical version: [en/operations.md](../en/operations.md)

## Требования ProductCatalog data exchange

Обмен данными ProductCatalog доступен только через HTTP. MCP-клиент должен передать
MCP-scoped API key, аутентифицированную Keycloak-сессию и ровно один заголовок
`X-Workspace-Id`. Отсутствующие, пустые, невалидные или повторяющиеся значения
Workspace отклоняются до downstream-вызова. Stdio и глобальное выполнение без явно
выбранного Workspace намеренно работают в fail-closed режиме.

## Нативные OAuth-клиенты

Канонический production resource — строго `https://thetea.app/mcp` без завершающего
слеша. RFC 9728 metadata доступна по адресу
`https://thetea.app/.well-known/oauth-protected-resource/mcp` и публикует только
внешний Keycloak issuer и resource scope `mcp:tools`. HTTP MCP сохраняет две
независимые границы: OAuth даёт identity и realm role, а `X-API-Key` — MCP scope
и разрешения `mcp:read` / `mcp:write`. Scope `mcp:tools` активирует Keycloak
audience mapper, но не заменяет эти authorization gates.

```bash
codex mcp add gzdkh-storefront \
  --url https://thetea.app/mcp \
  --oauth-client-id dkh-codex-local \
  --oauth-resource https://thetea.app/mcp
codex mcp login gzdkh-storefront --scopes mcp:tools
```

Сам API-ключ нельзя записывать в `config.toml`: заголовки `X-API-Key` и, для
Workspace-зависимых инструментов, `X-Workspace-Id` должны ссылаться через
`env_http_headers` на переменные окружения.

Gateway должен доверять forwarded headers только от фактического reverse proxy
через `Platform:Network:KnownProxies` и принимать audience
`https://thetea.app/mcp` через `Platform:Auth:Keycloak:AdditionalAudiences`.
`Mcp:PublicEndpoint` должен быть строго равен `https://thetea.app/mcp`.
