# Historical gateway rule stash

This inert patch preserves the nonprotected part of local stash
`f9e6aae92a4ff6aa100da2b6432619d239252072` from base
`bd2c14a6209e7f8cc2a6f866e24d1c30d6f5b2ce`. It records 24 historical
Codex/Gemini rule symlink targets and an old `AGENTS.md` edit. It does not
activate those rules; review their relevance against the current shared rules
before any use.

The patch excludes `CLAUDE.md`, all `.claude` paths and 11 untracked Claude
files. The original full stash and its standalone private backup are the sources
for those excluded paths; this patch is not a complete stash backup. The index
parent matched the base for all 25 included paths.

To inspect the saved versions, copy `nonprotected.patch` to a private location.
In a disposable checkout of the exact base, run
`git apply --index --unidiff-zero /path/to/nonprotected.patch`. Do not apply it
to an active checkout without review. The patch preserves Git symlink modes and
blob identities; the base is reachable from the `main` history.

Patch SHA-256: `7cff80f48dcc40373e2bf203d7b27408efb88813fd41efa46fac8336d0165be1`.
