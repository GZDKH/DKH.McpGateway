## Why

R8.1 requires a verified disposition for Catalog V1 receivers. On deployed McpGateway commit b460281, ProductStatsTool and ProductReviewRankingTool call Catalog ProductManagement V1. Catalog cae4503d supplies ProductManagement V2 via published Contracts 6.4.0. The existing workspace propagation and ranking fixtures cover those tool entry points. The linked owner issue is gzdkh/gateways/DKH.McpGateway#31.

## What Changes

Integrate V2 factual search for product statistics and review ranking with a distinct configured client. The remaining detail, origin and mutation receivers are tracked in the same issue. Origin has no equivalent field in V2 product detail and requires a separate owner decision.

## Capabilities

### New Capabilities

- `catalog-product-management-receivers`: Route MCP factual Catalog reads to the current owner contract.

## Impact

The change affects McpGateway Catalog clients and configuration. Infrastructure must supply the container endpoint before rollout.
