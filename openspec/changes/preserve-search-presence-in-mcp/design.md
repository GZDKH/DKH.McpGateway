## Context and decisions

See proposal and exact scoped Gate1 receipt. Reuse actual static tools and current guard/client/serialization boundaries. Shared projection maps the three already exposed financial fields (price, currency, stock) to nullableJSON without using scalar defaults for absence. The otherfourSDKfinancialfields stay unexposed; consuming optional encoding does not authorize expanding data exposure. Explicit zero, false and empty string remain supplied values. Preserve existing quote policy: price is withheld when explicitlyCallForPrice=true; do not add a new quote flag or raw price bypass to the established JSON surface. Other exposed nullablefields use their ownHas checks; no requestcurrency fallback or financialauthority is introduced.

Use one small shared receiving projection for all three search/recommendation surfaces, preserving existing core content/SEO fallback and recommendationdistance/ranking/dedup/seedguard. Genericreadpermission remains first. Publicstorefront catalogscopes remain derived through existinggate/visiblebindings. A missinggenericcatalogscope fails beforeRPC rather than inventing one. Legacypricearguments failbeforeRPC; do not silently drop them. Semantic mode calls existing HybridSearch withSEMANTIC andsamefilter, boundedpage/perpage/candidates/timeout; rawvectorcontrols are never emitted. Keyword mode retains existingSearchProducts path and contentfilters.

## Verification and rollout

Actual static tool paths receive genuine protobuf responses cloned/binaryparsed through publishedSDK1.12. Maintained regressions cover missing/default/nondefault values, existingquotepolicy, correctoutgoinglexical/semanticfilters/scopes, price/missingscope/boundsrefusals, cancellation/failure and existingauth/storefrontisolation. Newtests must reach realproduction tool/mapping logic. Finiteindependent completebatchreview thenONE normalfullprepush precede sourceCI/main/runtime qualification; filteredsubsets are notcompleteprojects. Include existing pinnedOpenSpecevidenceCI withtheownchange; keep taskmap structuralunder noMarkdownTODO instruction.

All receivingrepos must beready beforeSearch192merge; Searchoptionalmetadata precedesWorker100NULLproducer. No zero-use/globalownership/originalCatalogacceptance followsfromsourceabsence orpackagepins. Catalog581 remainsDraft/all9OPEN/noPartialMerge.

## Preserved JSON boundary

The initialdraft mentioned exposingall7SDKvalues. Existingcode/quote tests show this would add4previouslyunexposedfinancialfields. Correct the draft to keep the established response boundary: onlyprice/currency/stock are output, withactualpresence andexistingquote-pricewithholding; all7 can be supplied/cleared on genuineSDKinput without creating newJSONfields. This is a source-scoped correction before publication, not expanded financial authority.

## Additional actual receiving boundaries

The same cohesive receiving change covers retained Catalog ProductDetail money exposed by catalog resource, generic get-product and storefront get-product. Product price/currency and generic variant adjustment/stock JSON keys remain with null values; retained nonzero Catalog fields are not a qualified commercial offer or availability authority. Preserve content, origin, SKU, canonical Counterparty brand, workspace headers, existing read permissions and storefront gate/catalog traversal. Genuine Cart/Commerce-owned prices are unaffected.

Assistant unary/stream share AssistantMapper. Once genuine additive SDK1.7.0 is published by necessary predecessor Assistant27/MR126, read HasPrice/HasCurrency/HasInStock while retaining the existing optional oldPrice and content surface. No shadow SDK; actual producer absence remains a separate coordinated Assistant runtime change. Existing twenty-file review is conserved for unchanged source; review only the necessary supplemental delta before one final full pre-push.
