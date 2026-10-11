"""Pydantic v2 contracts for the ZArchitecture FormNode tree and form runtime context.

The tree is canonical for API/AI exchange, while database persistence stays relational.
Use `FormSchema.model_json_schema()` as the structured-output schema for PydanticAI.
"""
from __future__ import annotations

import re
from datetime import datetime
from typing import Any
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator
from dataclasses import dataclass
from pydantic_ai import Agent, RunContext

KEY_PATTERN = re.compile(r"^[a-z][a-z0-9_]*$")


class FormNodeSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: UUID | None = Field(default=None, description="Stable FormNode identity, if known")
    renderer_key: str = Field(min_length=1, description="Registered renderer, e.g. section, input.text, display.url")
    label: str | None = None
    description: str | None = None
    position: int = Field(default=0, ge=0)
    min_occurs: int = Field(default=1, ge=0)
    max_occurs: int = Field(default=1, ge=0, description="0 means unbounded, if supported by the runtime")
    own_security_scope: bool = False
    configuration: dict[str, Any] = Field(default_factory=dict)
    children: dict[str, FormNodeSchema] = Field(default_factory=dict)

    @field_validator("renderer_key")
    @classmethod
    def renderer_key_not_blank(cls, value: str) -> str:
        if not value.strip():
            raise ValueError("renderer_key must not be blank")
        return value

    @field_validator("children")
    @classmethod
    def valid_children_keys(cls, value: dict[str, FormNodeSchema]) -> dict[str, FormNodeSchema]:
        for key in value:
            if not KEY_PATTERN.fullmatch(key):
                raise ValueError(f"Invalid technical key: {key!r}")
        return value

    @model_validator(mode="after")
    def structural_constraints(self) -> FormNodeSchema:
        if self.max_occurs != 0 and self.max_occurs < self.min_occurs:
            raise ValueError("max_occurs must be >= min_occurs (or 0 for unbounded)")
        if self.children and self.renderer_key != "section":
            raise ValueError("Only section nodes may contain children in this initial contract")
        return self


class FormSchema(BaseModel):
    """Tree-shaped definition; children keys ARE FormNode technical keys."""
    model_config = ConfigDict(extra="forbid")

    form_version_id: UUID | None = None
    children: dict[str, FormNodeSchema] = Field(default_factory=dict)

    @field_validator("children")
    @classmethod
    def valid_root_keys(cls, value: dict[str, FormNodeSchema]) -> dict[str, FormNodeSchema]:
        for key in value:
            if not KEY_PATTERN.fullmatch(key):
                raise ValueError(f"Invalid technical key: {key!r}")
        return value

    @model_validator(mode="after")
    def unique_node_ids(self) -> FormSchema:
        seen: set[UUID] = set()
        def walk(children: dict[str, FormNodeSchema]) -> None:
            for node in children.values():
                if node.id is not None:
                    if node.id in seen:
                        raise ValueError(f"Duplicate FormNode id: {node.id}")
                    seen.add(node.id)
                walk(node.children)
        walk(self.children)
        return self


class EntityId(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: UUID


class WorkflowStateRef(EntityId):
    name: str | None = None


class WorkflowRef(EntityId):
    state: WorkflowStateRef


class ActorRef(EntityId):
    email: str | None = None


class FormHistoryEntry(BaseModel):
    """A completed transition: state means source state before transition."""
    model_config = ConfigDict(extra="forbid")
    state: WorkflowStateRef
    submission_id: UUID  # immutable archive of the source state's submission
    who: ActorRef
    note: str | None = None  # public note only
    when: datetime


class FormDataContext(BaseModel):
    model_config = ConfigDict(extra="forbid")
    form: dict[str, Any] = Field(default_factory=dict)
    request: EntityId | None = None
    submission: EntityId | None = None
    workflow: WorkflowRef | None = None
    history: list[FormHistoryEntry] = Field(default_factory=list)

    @model_validator(mode="after")
    def history_is_chronological(self) -> FormDataContext:
        if any(self.history[i].when > self.history[i+1].when for i in range(len(self.history)-1)):
            raise ValueError("history must be in chronological order")
        return self


def make_empty_form_data(schema: FormSchema) -> dict[str, Any]:
    """Build shape only. Actual typed values come from authorized Submission values."""
    def build(nodes: dict[str, FormNodeSchema]) -> dict[str, Any]:
        result: dict[str, Any] = {}
        for key, node in nodes.items():
            if node.renderer_key == "section":
                obj = build(node.children)
                result[key] = [] if node.max_occurs != 1 else obj
            elif node.renderer_key.startswith("input."):
                result[key] = None
        return result
    return build(schema.children)



class DesignContext(BaseModel):
    """Input supplied by ZArchitecture 88: canonical hierarchical FormSchema."""
    form_version_id: UUID
    form_schema: FormSchema
    component_catalog: list[dict[str, Any]] = Field(default_factory=list)


class DesignRequest(BaseModel):
    contract: str
    request_id: str
    instruction: str = Field(min_length=1)
    context: DesignContext


class DesignResponse(BaseModel):
    contract: str
    request_id: str
    result: FormSchema
    warnings: list[str] = Field(default_factory=list)


@dataclass
class FormDesignerDeps:
    request: DesignRequest
    # Future: authenticated ZArchitecture API client for on-demand retrieval.
    api_client: Any = None


def validate_proposal(schema: FormSchema, request: DesignRequest) -> list[str]:
    """Reject unsupported renderers and missing/re-identified existing nodes."""
    catalog = {
        c.get("renderer_key") for c in request.context.component_catalog
        if c.get("renderer_key")
    }
    original: dict[str, tuple[str, str, str]] = {}
    proposed: dict[str, tuple[str, str, str]] = {}
    problems: list[str] = []

    def collect(nodes: dict[str, FormNodeSchema], output: dict[str, tuple[str, str, str]], parent_id: str = "", *, proposal: bool = False):
        for key, node in nodes.items():
            if proposal and node.renderer_key not in catalog:
                problems.append(f"Unsupported renderer {node.renderer_key!r} at {key!r}")
            if node.id is not None:
                output[str(node.id)] = (key, parent_id, node.renderer_key)
            collect(node.children, output, str(node.id) if node.id else "", proposal=proposal)

    collect(request.context.form_schema.children, original)
    collect(schema.children, proposed, proposal=True)
    for node_id, old in original.items():
        current = proposed.get(node_id)
        if current is None:
            problems.append(f"Existing node {node_id} missing from result")
        elif current != old:
            problems.append(f"Existing node {node_id} changed key, parent or renderer")

    return problems


def createAgent(model):
    agent = Agent(
        model,
        deps_type=FormDesignerDeps,
        output_type=FormSchema,
        instructions=(
            "Jsi návrhář formulářů ZArchitecture. Vrať CELÝ strom FormSchema, "
            "nikoliv pouze změny. Prohlédni si nástroji stávající uzly a katalog. "
            "Používej výhradně renderer_key z katalogu. Zachovej všechny "
            "existující uzly, jejich id, technické klíče a rodiče. "
            "Pro nové uzly nech id=null (UUID přiřadí aplikace). "
            "Technické klíče používej malé ASCII a podtržítka. "
            "Do configuration patří konfigurace rendereru."
        ),
    )

    @agent.tool
    def get_current_form(ctx: RunContext[FormDesignerDeps]) -> FormSchema:
        """Return the existing hierarchical FormSchema including stable node IDs."""
        return ctx.deps.request.context.form_schema

    @agent.tool
    def list_form_components(ctx: RunContext[FormDesignerDeps]) -> list[dict[str, Any]]:
        """List renderer types that can be used in the generated form."""
        return ctx.deps.request.context.component_catalog

    @agent.tool
    def get_form_component(ctx: RunContext[FormDesignerDeps], renderer_key: str) -> dict[str, Any]:
        """Get the component definition including configuration example if available."""
        for component in ctx.deps.request.context.component_catalog:
            if component.get("renderer_key") == renderer_key:
                return component
        return {"error": "Unknown or inaccessible renderer_key", "renderer_key": renderer_key}

    return agent
