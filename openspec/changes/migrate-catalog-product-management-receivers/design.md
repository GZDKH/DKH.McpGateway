## Context

Both search tools already use caller-scoped Catalog requests. ProductManagement V1 remains present for other registered tools during the transition.

## Decisions

- Use the generated V2 client with its own configuration key. It points to the same Catalog service and keeps the existing request metadata and cancellation handling.
- Preserve the MCP output shapes for statistics and ranking. Keep origin and mutation routing open until their owner dispositions are available.

## Risks

The deployed configuration must resolve the new endpoint to Catalog. The package requires exact source CI, an authenticated receiver trace and a completed review before cutover.
