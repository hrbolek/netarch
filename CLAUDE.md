# Claude Code adapter

The canonical repository instructions are in [`AGENTS.md`](AGENTS.md). Read
them first; do not copy or redefine architecture rules here.

For role-specific work, read the corresponding provider-neutral specification
in [`agent-roles/`](agent-roles/) and relevant portable skills in
[`.agents/skills/`](.agents/skills/). Claude Code subagent profiles, when used,
are thin tool/delegation adapters under `.claude/agents/`.

All agent use is optional. Ordinary development and deterministic validation
must not depend on Claude Code, an LLM provider, hooks, or model credentials.
