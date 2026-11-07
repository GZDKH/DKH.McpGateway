## ADDED Requirements

### Requirement: MCP factual product search uses the current Catalog owner

Product statistics and review ranking SHALL use Catalog ProductManagement V2 while preserving existing caller authorization, workspace propagation and response semantics.

#### Scenario: Workspace-scoped product statistics

- **WHEN** an authorized caller requests product statistics for a valid workspace
- **THEN** the gateway queries Catalog V2 with that workspace and retains the existing category and product count response

#### Scenario: Review ranking joins current Catalog facts

- **WHEN** an authorized caller requests product review ranking
- **THEN** the gateway selects products through Catalog V2 and applies the existing review ranking and refusal behavior

### Requirement: MCP factual product detail preserves its access boundary

Merchant catalog resources and storefront product detail and recommendations SHALL use Catalog ProductManagement V2 with their existing access scope and response shapes.

#### Scenario: Read factual product details

- **WHEN** an authorized merchant or storefront caller requests factual product detail
- **THEN** the gateway preserves workspace or storefront catalog scope and cancellation, resolves display names from eligible counterparty links, and represents unavailable money as null

#### Scenario: Find a storefront recommendation seed

- **WHEN** a storefront caller asks for product recommendations
- **THEN** the gateway looks up the seed only in that storefront's catalogs and preserves the existing search and refusal behavior
