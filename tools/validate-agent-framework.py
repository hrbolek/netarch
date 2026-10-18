#!/usr/bin/env python3
"""Validate shared ZArchitecture role, skill, rule, and thin adapter metadata."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parent.parent
ROLE_DIR = ROOT / "agent-roles"
SKILLS = ROOT / ".agents" / "skills"
COPILOT_SKILLS = ROOT / ".github" / "skills"
COPILOT_ADAPTERS = ROOT / ".github" / "agents"
COPILOT_INSTRUCTIONS = ROOT / ".github" / "copilot-instructions.md"
CLAUDE_ADAPTERS = ROOT / ".claude" / "agents"
RULES = ROOT / "docs" / "architecture" / "rules.yml"
EXPECTED_ROLES = {
    "zarchitecture-orchestrator",
    "architecture-guardian",
    "generic-architecture-engineer",
    "entity-developer",
    "security-architect",
    "api-services-engineer",
    "razor-ui-engineer",
    "test-quality-engineer",
    "documentation-architect",
    "migration-refactoring-engineer",
}
EXPECTED_SKILLS = {
    "architecture-discovery",
    "generic-abstraction-discovery",
    "entity-implementation",
    "generic-service-extension",
    "generic-resolver-extension",
    "razor-component-reuse",
    "rbac-authorization-checks",
    "architecture-compliance",
    "architecture-regression-testing",
}
COPILOT_TOOLS = {"search", "read", "edit", "execute", "agent"}
CLAUDE_TOOLS = {"Read", "Grep", "Glob", "Edit", "Write", "Bash", "Agent"}
MAX_ADAPTER_BODY_CHARS = 500


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def frontmatter(path: Path) -> tuple[dict[str, str], str, str]:
    text = path.read_text(encoding="utf-8")
    parts = text.split("---", 2)
    if len(parts) != 3 or parts[0].strip():
        fail(f"{path.relative_to(ROOT)}: expected YAML frontmatter delimited by ---")
    data: dict[str, str] = {}
    for line in parts[1].splitlines():
        if not line.strip() or line.lstrip().startswith("#") or line.startswith("  "):
            continue
        match = re.fullmatch(r"([a-z][a-zA-Z0-9_-]*):\s*(.*)", line)
        if not match:
            fail(f"{path.relative_to(ROOT)}: invalid frontmatter field: {line}")
        key, value = match.groups()
        if key in data:
            fail(f"{path.relative_to(ROOT)}: duplicate frontmatter field {key}")
        data[key] = value
    return data, parts[2], parts[1]


def scalar(value: str, path: Path) -> str:
    if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
        return value[1:-1]
    if not value or value.startswith("[") or value.startswith("{"):
        fail(f"{path.relative_to(ROOT)}: expected a non-empty scalar")
    return value


def inline_list(value: str, path: Path, key: str) -> list[str]:
    if not (value.startswith("[") and value.endswith("]")):
        fail(f"{path.relative_to(ROOT)}: {key} must be an inline YAML list")
    matches = re.findall(r"'([^']+)'|\"([^\"]+)\"", value[1:-1])
    items = [single or double for single, double in matches]
    if not items or len(items) != len(matches) or len(items) != len(set(items)):
        fail(f"{path.relative_to(ROOT)}: malformed, empty, or duplicate {key} value")
    return items


def handoff_targets(yaml: str, path: Path) -> list[str]:
    match = re.search(r"(?m)^handoffs:\s*\n((?:  - .*\n(?:    .*\n)*)+)", yaml)
    if not match:
        return []
    targets: list[str] = []
    for block in re.split(r"(?m)^  - ", match.group(1)):
        if not block.strip():
            continue
        fields: dict[str, str] = {}
        for index, line in enumerate(block.splitlines()):
            pattern = r"([a-z][a-z0-9_-]*):\s*(.*)" if index == 0 else r"    ([a-z][a-z0-9_-]*):\s*(.*)"
            field = re.fullmatch(pattern, line)
            if not field:
                fail(f"{path.relative_to(ROOT)}: invalid handoff field: {line}")
            key, value = field.groups()
            if key in fields:
                fail(f"{path.relative_to(ROOT)}: duplicate handoff field {key}")
            fields[key] = value
        if not {"label", "agent", "prompt"} <= fields.keys():
            fail(f"{path.relative_to(ROOT)}: handoff requires label, agent, and prompt")
        if fields.get("send", "false") not in {"true", "false"}:
            fail(f"{path.relative_to(ROOT)}: handoff send must be true or false")
        targets.append(fields["agent"])
    return targets


def validate_role_specs() -> None:
    paths = sorted(ROLE_DIR.glob("*.md"))
    names = {path.stem for path in paths}
    if names != EXPECTED_ROLES:
        fail(f"canonical role specs mismatch; missing={sorted(EXPECTED_ROLES - names)}, extra={sorted(names - EXPECTED_ROLES)}")
    for path in paths:
        if not path.read_text(encoding="utf-8").startswith("# "):
            fail(f"{path.relative_to(ROOT)}: expected a Markdown role heading")


def validate_adapters(directory: Path, suffix: str, tools_allowed: set[str], vendor: str) -> None:
    paths = sorted(directory.glob(f"*{suffix}"))
    names: set[str] = set()
    if len(paths) != len(EXPECTED_ROLES):
        fail(f"{directory.relative_to(ROOT)}: expected {len(EXPECTED_ROLES)} {vendor} adapters, found {len(paths)}")
    for path in paths:
        data, body, yaml = frontmatter(path)
        for required in ("name", "description", "tools"):
            if required not in data:
                fail(f"{path.relative_to(ROOT)}: missing {required}")
        name = scalar(data["name"], path)
        description = scalar(data["description"], path)
        if name in names or name not in EXPECTED_ROLES:
            fail(f"{path.relative_to(ROOT)}: duplicate or unknown adapter name {name}")
        names.add(name)
        if not description or not body.strip() or len(body.strip()) > MAX_ADAPTER_BODY_CHARS:
            fail(f"{path.relative_to(ROOT)}: adapter body must be non-empty and <= {MAX_ADAPTER_BODY_CHARS} characters")
        if f"agent-roles/{name}.md" not in body or "AGENTS.md" not in body or ".agents/skills/" not in body:
            fail(f"{path.relative_to(ROOT)}: adapter must reference shared instructions, its canonical role, and portable skills")
        if vendor == "Copilot":
            tools = inline_list(data["tools"], path, "tools")
        else:
            tools = [tool.strip() for tool in data["tools"].split(",") if tool.strip()]
        if not tools or set(tools) - tools_allowed:
            fail(f"{path.relative_to(ROOT)}: unsupported or empty {vendor} tool list")
        if name == "architecture-guardian" and set(tools) & {"edit", "execute", "agent", "Edit", "Write", "Bash", "Agent"}:
            fail(f"{path.relative_to(ROOT)}: Architecture Guardian must be configured read-only")
        if vendor == "Copilot" and "agents" in data:
            allowed_agents = inline_list(data["agents"], path, "agents")
            if set(allowed_agents) - EXPECTED_ROLES or "agent" not in tools:
                fail(f"{path.relative_to(ROOT)}: unknown subagent or agents list without agent tool")
            profile_names = {
                scalar(frontmatter(adapter)[0].get("name", ""), adapter)
                for adapter in paths
            }
            if set(allowed_agents) - profile_names:
                fail(f"{path.relative_to(ROOT)}: subagent reference has no matching Copilot profile")
            if name == "zarchitecture-orchestrator" and set(allowed_agents) != EXPECTED_ROLES - {name}:
                fail(f"{path.relative_to(ROOT)}: orchestrator must allow-list all and only the nine specialist profiles")
        for target in handoff_targets(yaml, path):
            if target not in EXPECTED_ROLES:
                fail(f"{path.relative_to(ROOT)}: unknown handoff target {target}")
    if names != EXPECTED_ROLES:
        fail(f"{vendor} adapter names do not match canonical roles")


def validate_skills() -> None:
    required_sections = (
        "## When to use",
        "## Prerequisites",
        "## Procedure",
        "## Architectural constraints",
        "## Validation",
        "## Expected outputs",
    )
    for directory, label in ((SKILLS, "portable"), (COPILOT_SKILLS, "Copilot")):
        paths = sorted(directory.glob("*/SKILL.md"))
        names: set[str] = set()
        for path in paths:
            data, body, _ = frontmatter(path)
            for required in ("name", "description"):
                if required not in data:
                    fail(f"{path.relative_to(ROOT)}: missing {required}")
            name = scalar(data["name"], path)
            description = scalar(data["description"], path)
            if name in names or path.parent.name != name or not description or not body.strip():
                fail(f"{path.relative_to(ROOT)}: duplicate/mismatched skill name or empty content")
            if any(section not in body for section in required_sections):
                fail(f"{path.relative_to(ROOT)}: incomplete skill procedure sections")
            names.add(name)
        if names != EXPECTED_SKILLS:
            fail(f"{label} skills mismatch; missing={sorted(EXPECTED_SKILLS - names)}, extra={sorted(names - EXPECTED_SKILLS)}")

    for name in EXPECTED_SKILLS:
        portable = SKILLS / name / "SKILL.md"
        copilot = COPILOT_SKILLS / name / "SKILL.md"
        if portable.read_bytes() != copilot.read_bytes():
            fail(f"Copilot skill is out of sync with portable source: {name}")


def validate_rules() -> None:
    if not RULES.is_file():
        fail("missing docs/architecture/rules.yml")
    text = RULES.read_text(encoding="utf-8")
    ids = re.findall(r"^\s+- id:\s*(ZA-[A-Z]+-\d{3})\s*$", text, re.M)
    rule_ids = [item for item in ids if "-REC-" not in item]
    if len(rule_ids) < 8 or len(rule_ids) != len(set(rule_ids)):
        fail("architecture rules must have at least 8 unique stable ZA rule IDs")
    for required in ("title:", "severity:", "contract:", "evidence:"):
        if required not in text:
            fail(f"architecture rules missing field {required}")
    in_evidence = False
    for line in text.splitlines():
        if re.match(r"^\s+evidence:\s*$", line):
            in_evidence = True
            continue
        if re.match(r"^\s+(verification|contract|severity|title):", line):
            in_evidence = False
        if in_evidence:
            match = re.match(r"^\s+- ((?:src|templates|tests|tools|\.github|docs)/[^ ]+|AGENTS\.md)\s*$", line)
            if match and not (ROOT / match.group(1).rstrip("/")).exists():
                fail(f"architecture evidence path does not exist: {match.group(1)}")


def validate_shared_entry_points() -> None:
    agents_md = ROOT / "AGENTS.md"
    if not agents_md.is_file():
        fail("missing primary AGENTS.md entry point")
    instructions = agents_md.read_text(encoding="utf-8")
    if "agent-roles/" not in instructions or ".agents/skills/" not in instructions:
        fail("AGENTS.md must point to canonical roles and portable skills")

    if not COPILOT_INSTRUCTIONS.is_file():
        fail("missing .github/copilot-instructions.md")
    copilot_text = COPILOT_INSTRUCTIONS.read_text(encoding="utf-8")
    if "AGENTS.md" not in copilot_text or "agent-roles/" not in copilot_text or ".agents/skills/" not in copilot_text:
        fail("Copilot instructions must reference shared instructions, canonical roles, and portable skill sources")
    if "optional" not in copilot_text.lower() or "architecture-guardian" not in copilot_text:
        fail("Copilot instructions must preserve optional routing and independent architecture review")
    missing_agents = sorted(name for name in EXPECTED_ROLES if name not in copilot_text)
    if missing_agents:
        fail(f"Copilot instructions omit specialist routing for: {', '.join(missing_agents)}")

    for directory in (ROLE_DIR, COPILOT_ADAPTERS):
        for path in directory.glob("*.md"):
            text = path.read_text(encoding="utf-8")
            for skill in re.findall(r"\.agents/skills/([a-z][a-z0-9-]*)/?", text):
                if skill not in EXPECTED_SKILLS or not (SKILLS / skill / "SKILL.md").is_file():
                    fail(f"{path.relative_to(ROOT)} references an unknown portable skill: {skill}")

    claude = ROOT / "CLAUDE.md"
    if not claude.is_file():
        fail("missing Claude Code adapter CLAUDE.md")
    claude_text = claude.read_text(encoding="utf-8")
    if "AGENTS.md" not in claude_text or "agent-roles/" not in claude_text:
        fail("CLAUDE.md must reference shared instructions and canonical roles")

    cursor = ROOT / ".cursor" / "rules" / "zarchitecture.mdc"
    data, body, _ = frontmatter(cursor)
    if data.get("alwaysApply") != "true" or "AGENTS.md" not in body or "agent-roles/" not in body:
        fail("Cursor adapter must be an index to shared instructions and roles")


def validate_whitespace() -> None:
    paths = [
        ROOT / "AGENTS.md",
        ROOT / "CLAUDE.md",
        COPILOT_INSTRUCTIONS,
        ROOT / ".github" / "workflows" / "architecture-audit.yml",
        ROOT / ".cursor" / "rules" / "zarchitecture.mdc",
        ROOT / "tools" / "validate-agent-framework.py",
    ]
    paths.extend(ROLE_DIR.glob("*.md"))
    paths.extend(SKILLS.glob("*/SKILL.md"))
    paths.extend(COPILOT_SKILLS.glob("*/SKILL.md"))
    paths.extend(COPILOT_ADAPTERS.glob("*.agent.md"))
    paths.extend(CLAUDE_ADAPTERS.glob("*.md"))
    paths.extend((ROOT / "docs" / "architecture").rglob("*"))
    for path in paths:
        if not path.is_file():
            continue
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            if line.endswith((" ", "\t")):
                fail(f"{path.relative_to(ROOT)}:{line_number}: trailing whitespace")


def main() -> None:
    validate_role_specs()
    validate_adapters(COPILOT_ADAPTERS, ".agent.md", COPILOT_TOOLS, "Copilot")
    validate_adapters(CLAUDE_ADAPTERS, ".md", CLAUDE_TOOLS, "Claude Code")
    validate_skills()
    validate_rules()
    validate_shared_entry_points()
    validate_whitespace()
    print(f"Agent framework valid: {len(EXPECTED_ROLES)} canonical roles, "
          f"{len(EXPECTED_SKILLS)} synchronized portable/Copilot skills, Copilot/Claude adapters, "
          "and stable architecture rules.")


if __name__ == "__main__":
    main()
