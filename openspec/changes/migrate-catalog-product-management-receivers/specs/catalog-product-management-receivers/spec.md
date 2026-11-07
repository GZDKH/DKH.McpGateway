## ADDED Requirements

### Requirement: MCP factual product search uses the current Catalog owner

Product statistics and review ranking SHALL use Catalog ProductManagement V2 while preserving existing caller authorization, workspace propagation and response semantics.

#### Scenario: Workspace-scoped product statistics

- **WHEN** an authorized caller requests product statistics for a valid workspace
- **THEN** the gateway queries Catalog V2 with that workspace and retains the existing category and product count response

#### Scenario: Review ranking joins current Catalog facts

- **WHEN** an authorized caller requests product review ranking
- **THEN** the gateway selects products through Catalog V2 and applies the existing review ranking and refusal behavior
