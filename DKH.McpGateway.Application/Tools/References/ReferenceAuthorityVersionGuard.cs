namespace DKH.McpGateway.Application.Tools.References;

internal static class ReferenceAuthorityVersionGuard
{
    internal static bool TryGetMutationIdentity(
        string? stableId,
        long? expectedAuthorityVersion,
        out GuidValue id,
        out long expected,
        out string error)
    {
        id = new GuidValue();
        if (!Guid.TryParse(stableId, out var parsedId))
        {
            expected = expectedAuthorityVersion.GetValueOrDefault();
            error = "A valid stableId returned by get/list is required for update/delete.";
            return false;
        }

        expected = expectedAuthorityVersion.GetValueOrDefault();
        if (expected <= 0)
        {
            error = "A positive expectedAuthorityVersion returned by get/list is required for update/delete.";
            return false;
        }

        id = new GuidValue(parsedId.ToString("D"));
        error = string.Empty;
        return true;
    }
}
