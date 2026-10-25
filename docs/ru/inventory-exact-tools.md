# Точные MCP-инструменты Inventory

Каноническая спецификация и формы запросов: [English](../en/inventory-exact-tools.md).

Все четыре имени остаются приватными admin tools. Доступность читается по точным stock workspace, owner counterparty, warehouse и SellableUnit; Product/Variant не являются заменой. Мутации требуют настоящих исходных Cart bytes/SHA, operation ID и положительного текущего CAS. Списки общих остатков, прямые корректировки и alerts явно выведены из эксплуатации и не вызывают v1.

Исходный HTTP HUMAN bearer и выбранный workspace передаются через Platform; актуальные полномочия независимо проверяет Inventory. Изменение caller/claims/bearer/workspace, отмена или истечение токена после await/disposal запрещают выдачу ответа. Отказ выдачи после мутации не означает rollback: нельзя слепо повторять её с новым ID. Сервисные профили, реальные grant и runtime-приёмка этим изменением не выдаются.

HUMAN определяется через существующий Platform HttpCurrentUser и принятые Inventory issuer/sub/preferred_username: service-account и неназванный principal не доходят до RPC даже с admin role и API key. GUID/имя классифицируют личность, а не выдают права на остатки. Принимается опубликованный Common1.2.3 с автоматическим bootstrap известного protobuf cache race; локального переключателя или отключения параллельных тестов нет. Опубликованный артефакт не означает, что весь producer pipeline47733 успешен.
