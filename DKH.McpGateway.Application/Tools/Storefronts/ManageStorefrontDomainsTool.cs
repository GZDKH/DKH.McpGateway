using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontDomainsTool
{
    [McpServerTool(Name = "manage_storefront_domains"), Description(
        "Manage custom domains for a storefront. " +
        "Actions: 'list' to view domains, 'add' to add a domain, " +
        "'remove' to delete, 'verify' to check DNS, 'set_primary' to set primary domain.")]
    public static async Task<string> ExecuteAsync(
        StorefrontCrudService.StorefrontCrudServiceClient crudClient,
        StorefrontDomainService.StorefrontDomainServiceClient domainClient,
        [Description("Storefront code (e.g. 'my-store')")] string storefrontCode,
        [Description("Action: list, add, remove, verify, or set_primary")] string action,
        [Description("Domain name (for add, e.g. 'shop.example.com')")] string? domain = null,
        [Description("Domain ID (for remove/verify/set_primary, returned from list)")] string? domainId = null,
        [Description("Set as primary domain (for add)")] bool? isPrimary = null,
        CancellationToken cancellationToken = default)
    {
        var storefront = await crudClient.GetByCodeAsync(
            new GetStorefrontByCodeRequest { Code = storefrontCode },
            cancellationToken: cancellationToken);

        if (storefront.Storefront is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Storefront '{storefrontCode}' not found" },
                McpJsonDefaults.Options);
        }

        var storefrontId = storefront.Storefront.Id;

        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            var response = await domainClient.GetDomainsAsync(
                new GetDomainsRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                domains = response.Domains.Select(d => new
                {
                    domainId = d.Id,
                    d.Domain,
                    d.IsPrimary,
                    d.IsVerified,
                    sslStatus = d.SslStatus.ToString(),
                    verificationToken = d.VerificationToken,
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "add", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(domain))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "domain is required for add (e.g. 'shop.example.com')" },
                    McpJsonDefaults.Options);
            }

            var response = await domainClient.AddDomainAsync(
                new AddDomainRequest
                {
                    StorefrontId = storefrontId,
                    Domain = domain,
                    IsPrimary = isPrimary ?? false,
                },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "added",
                domain = new
                {
                    domainId = response.Domain.Id,
                    response.Domain.Domain,
                    response.Domain.IsPrimary,
                    verificationToken = response.Domain.VerificationToken,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(domainId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "domainId is required for remove" },
                    McpJsonDefaults.Options);
            }

            var response = await domainClient.RemoveDomainAsync(
                new RemoveDomainRequest { StorefrontId = storefrontId, DomainId = domainId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "removed",
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "verify", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(domainId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "domainId is required for verify" },
                    McpJsonDefaults.Options);
            }

            var response = await domainClient.VerifyDomainAsync(
                new VerifyDomainRequest { StorefrontId = storefrontId, DomainId = domainId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "verified",
                isVerified = response.IsVerified,
                domain = new { domainId = response.Domain.Id, response.Domain.Domain },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "set_primary", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(domainId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "domainId is required for set_primary" },
                    McpJsonDefaults.Options);
            }

            var response = await domainClient.SetPrimaryAsync(
                new SetPrimaryDomainRequest { StorefrontId = storefrontId, DomainId = domainId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "set_primary",
                domain = new { domainId = response.Domain.Id, response.Domain.Domain, response.Domain.IsPrimary },
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: list, add, remove, verify, or set_primary" },
            McpJsonDefaults.Options);
    }
}
