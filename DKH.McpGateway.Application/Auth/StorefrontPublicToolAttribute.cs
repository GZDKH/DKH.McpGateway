namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Marks an MCP tool type as belonging to the <b>public storefront surface</b> — the read-only
/// <c>storefront_*</c> namespace reachable with a <see cref="ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1.ApiKeyScope.Storefront"/>
/// API key and no privileged bearer.
/// <para>
/// Surface classification is <b>explicit</b>, never inferred from the tool name: some administrative
/// tools (e.g. <c>storefront_overview</c>, <c>storefront_audit</c>) also start with <c>storefront_</c>
/// yet must stay on the admin surface. Only tool types carrying this attribute are exposed to
/// storefront-scoped keys; everything unmarked is treated as admin and denied to storefront keys
/// (default-closed).
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StorefrontPublicToolAttribute : Attribute;
