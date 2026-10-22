## Purpose

Allow actual MCP receivers to represent unknown discovery values truthfully and issue supported search requests while preserving permission and storefront isolation.

## ADDED Requirements

### Requirement: Tool projections preserve missing discovery values

Generic search, storefront search and storefront recommendation tools SHALL use actualSDK presence for their existing price/currency/stock JSON fields. OtherfourSDKfinancialfields MUST remain unexposed. Absent values MUST NOT become zero, false or requestcurrency. Explicit defaults SHALL remain distinct from absence. Existing quote-price withholding SHALL remain without expanding the existing JSON surface.

#### Scenario: Response omits financial values
- **WHEN** a genuine protobuf result omits all7 values and passes through any of the three actual tool paths
- **THEN** its existing price/currency/stock JSON values are unknown/null, unexposed fields remain absent and content IDs remain intact

#### Scenario: Response supplies explicit defaults or nondefaults
- **WHEN** genuineSDK1.12 binary/clone messages supply those fields
- **THEN** each exposed nullableJSON value reflects its own presence/value, subject to the existing quote-price withholding policy; otherfourfields remain unexposed

#### Scenario: Explicit quote flag with numeric price
- **WHEN** the existing quote policy receives CallForPrice=true alongside a supplied price
- **THEN** public display price remains null and no new financial fields are exposed

### Requirement: Search controls remain supported and scoped

Financial controls MUST be rejected before downstream retrieval. Lexical/semantic requests SHALL preserve exact publicscope and contentcontrols; semantic mode MUST use a supported boundedRPC without rawvectorcontrols. Existing read permission, storefrontgate/visiblebindings and recommendationseed isolation SHALL remain.

#### Scenario: Legacy price range is requested
- **WHEN** a caller supplies a price bound
- **THEN** it fails observably beforeRPC without silently dropping the requested condition

#### Scenario: Scoped lexical or semantic search is requested
- **WHEN** actual generic/storefront tools issue search
- **THEN** their correctRPC carries the same exact derived/providedscope and boundedcontrols, withno financialfilter orrawvectorquery

#### Scenario: Scope or semantic bounds are invalid
- **WHEN** a genericcatalogscope is missing or a semanticwindow exceeds the publicbound
- **THEN** retrieval is refused beforeRPC and no catalogidentity is invented

### Requirement: Retained Catalog fields do not establish commercial authority

Catalog resource, generic get-product and storefront get-product SHALL keep existing financial JSON keys as null instead of projecting retained Product price/currency. Generic variant adjustment/stock keys SHALL also remain null. Content, origin, SKU, canonical Counterparty names and existing caller/workspace/storefront restrictions SHALL remain.

#### Scenario: Retained positive or default values
- **WHEN** actual Catalog ProductDetail carries either positive or default legacy price/stock values through any of these three paths
- **THEN** existing financial keys remain null and product identity, categories, workspace metadata and cancellation remain preserved

### Requirement: Assistant cards preserve SDK presence through actual tools

Actual unary and stream assistant tools SHALL use genuine AssistantSDK1.7 presence independently for their existing price/currency/inStock JSON keys and retain existing optional oldPrice and content behavior.

#### Scenario: Assistant omits or clears financial values
- **WHEN** the genuine binary/clone card omits or clears values through either actual tool
- **THEN** missing values remain null without fabricating zero, availability or currency

#### Scenario: Assistant supplies explicit defaults or nondefaults
- **WHEN** a card explicitly supplies values through either actual tool
- **THEN** its JSON preserves each independent supplied value and existing permissions/request metadata remain unchanged
