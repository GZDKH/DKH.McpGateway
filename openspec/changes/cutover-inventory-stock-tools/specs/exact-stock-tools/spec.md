## ADDED Requirements

### Requirement: Exact current stock availability
The gateway SHALL send only explicit stock workspace, owner counterparty, warehouse and Sellable identities to Inventory v2, using the original admitted HUMAN bearer and selected workspace. Native current stock-owner admission SHALL remain independent.

#### Scenario: Qualified exact availability
- **WHEN** an authenticated HTTP MCP-admin caller supplies an exact scope matching its selected workspace
- **THEN** the native exact-availability RPC is called and its scoped genuine quantity/digest is returned, preserving zero

#### Scenario: Legacy or cross-scope input
- **WHEN** a caller supplies Product/Variant aliases, missing owner scope or a different selected workspace
- **THEN** no Inventory RPC is called and no owner/warehouse/Sellable or zero is invented

#### Scenario: Privileged machine or unnamed caller
- **WHEN** an authenticated service-account or unnamed principal has an MCP key, privileged role, bearer and expiry
- **THEN** no native stock RPC is called; identity classification does not replace independent current stock-owner authority

### Requirement: Immutable original reservation operations
The gateway SHALL use the genuine typed v2 reserve/get/get-operation/release/bind-order/dispatch contract, exact original Cart bytes and SHA, caller-supplied operation IDs and actual positive mutation CAS. It SHALL NOT create Cart/Order facts, accept client TTL, expose owner expiry execution, or retry an ambiguous mutation.

#### Scenario: Reserve and later mutation
- **WHEN** the caller supplies a valid authentic-original shape and operation-specific native selectors/CAS
- **THEN** the unchanged typed original/request bytes are sent once and the actual joined owner response is returned

#### Scenario: Unknown or denied owner outcome
- **WHEN** Inventory refuses, is unavailable, or cannot find an admitted original slot
- **THEN** the original failure is preserved, with no success/rollback/negative-stock inference

### Requirement: Final request-lifetime disclosure fence
The gateway SHALL preserve its original authenticated request/principal/claims/bearer/workspace/API-key admission and original expiry across each downstream await, including failure paths. It SHALL refuse disclosure if any bound input changes or the original request is cancelled/expired.

#### Scenario: Caller changes during owner read
- **WHEN** the same user ID is presented with changed claims, principal, bearer or workspace while an RPC is awaited
- **THEN** the response or error from the previous frame is not disclosed

### Requirement: Complete legacy stock retirement
All four MCP stock tools SHALL stop using Inventory v1 clients. Generic stock pages/direct adjustments/alerts without a faithful1.19 equivalent SHALL return explicit unsupported/retired guidance after ordinary MCP permission admission.

#### Scenario: Unsupported legacy operation
- **WHEN** an old generic stock/list/adjust/alert action is invoked
- **THEN** it makes no legacy RPC and explains the exact supported alternative or missing owner capability
