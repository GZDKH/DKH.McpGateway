# Exact Inventory MCP tools

Inventory Contracts1.19 replaces the old Product/Variant stock surface. All four tool names remain private MCP-admin tools. Their obsolete generic operations are explicitly retired; they do not call Inventory v1. This is a stateless consumer change, not an Inventory grant, migration, actual stock mutation or whole-plan acceptance.

| Tool/action | Native operation and required input |
| --- | --- |
| `query_stock availability` | `GetExactStockAvailability`: explicit stock-owner workspace, counterparty, warehouse and actual SellableUnit. The selected HTTP `X-Workspace-Id` must match the stock workspace. Zero is retained as zero. |
| `query_stock check_availability` | Same exact read plus a positive requested quantity; no Cart original is fabricated for this read. |
| `manage_reservation reserve` | Exact `ReserveExactStockRequest` JSON. Original reserve operation ID equals command operation ID; expected hold revision is absent. |
| `manage_reservation get/get_operation` | Exact immutable original Cart tuple; get-operation also supplies its actual operation ID. A native NotFound/Unavailable is not a successful release or rollback. |
| `manage_reservation release` | Actual original, hold ID, stable operation ID and actual positive expected hold revision. |
| `manage_reservation bind_order` | Same native original/CAS plus actual independently protected Order source selector and selected source line ID. |
| `manage_reservation confirm` | Explicit original allocation IDs/positive partial quantities, actual Order-line binding and CAS. This is physical dispatch, not Order completion. |
| `manage_stock`, `manage_stock_alert`, old `get_level/get_levels`, reservation `list` | Explicit unsupported/retired response. No faithful general on-hand/reserved page, direct stock adjustment or alert replacement exists in1.19. |

The `json` argument is the corresponding strict generated-protobuf request shape. Unknown legacy fields are refused. Keep the genuine original Cart UTF8 bytes as base64 in `original.originalCartFrame`, with their exact lowercase SHA256 in `original.cartFrameSha256`; never reserialize the frame. Opaque Cart IDs and raw line keys remain literal. Int64 revision values use protobuf JSON strings. Never invent IDs, choose an initial CAS, accept client TTL, infer stock owner from seller/Product, substitute a public manifest digest, or retry an ambiguous mutation with a new operation ID. Owner expiry execution is not exposed.

An admitted HTTP HUMAN bearer, MCP API-key permission and selected workspace are required. Existing Platform propagation forwards that original bearer; native Inventory independently checks its current stock-owner mandate and actual retained Cart/Order sources. Stdio or a historical token cannot substitute for the HTTP identity. The original caller, full claims, bearer, API-key admission, workspace, cancellation and original token expiry are fenced after every downstream await and disposal, including errors. A later refused disclosure can follow a committed mutation: it is not rollback proof. Reconcile the same original operation under fresh native authorization; do not blindly retry.

Configuration keeps the existing `StockReservationServiceClient` URL and registers its v2 generated type. Legacy StockQuery/StockManagement/LowStockAlert clients and default/local/Docker entries are removed. The Development reservation URL is corrected from the MCP port5013 to the Inventory port5014. No new endpoint key, service identity, grant, credential or production profile is introduced. Native current profiles/mandates and genuine runtime acceptance remain independent prerequisites.

Infrastructure companion configuration: the base and production `docker-compose/env/mcp-gateway.env` files still supply the three unused legacy URL keys at this source snapshot. The Infrastructure owner must remove those overrides to make resolved production configuration one-for-one; this gateway change does not edit another owner's repository or claim that rollout complete. Their presence does not register or invoke a v1 client. The existing reservation endpoint remains unchanged in production.

HUMAN classification reuses Platform `HttpCurrentUser` and the Inventory producer's accepted authenticated identity shape: one nonempty issuer, one actual GUID subject consistent with Platform current identity, and one nonblank `preferred_username` outside the reserved `service-account-` prefix. A privileged role, API key, GUID or username is not a stock-owner permission. Machine/unnamed callers are stopped before every native stock RPC; native current action/owner admission remains mandatory.

The consumer directly receives the already published `DKH.Platform.Grpc.Common`1.2.3 automatic module bootstrap for the known Google3.36.1 cold-descriptor cache race, plus the existing Identity1.8 reader. It does not implement a local cache toggle or serialize tests. This is the same genuine package/archive6e07764b... previously qualified by its owner; package metadata points main47733, which is failed overall, so no producer whole-main success is inferred from artifact availability.
