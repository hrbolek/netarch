"""Minimal PydanticAI design agent. No direct access to ZArchitecture database."""
from __future__ import annotations
import os
import re
from typing import Any
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field, model_validator
from pydantic_ai import Agent

app = FastAPI(title="Form design AI agent")
KEY_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")

class FormNode(BaseModel):
    renderer_key: str
    label: str
    description: str | None = None
    configuration: dict[str, Any] = Field(default_factory=dict)
    children: dict[str, "FormNode"] = Field(default_factory=dict)

class FormSchema(BaseModel):
    children: dict[str, FormNode] = Field(default_factory=dict)

    @model_validator(mode="after")
    def valid_keys(self):
        def walk(children: dict[str, FormNode]):
            for key, node in children.items():
                if not KEY_RE.fullmatch(key):
                    raise ValueError(f"Invalid technical key: {key}")
                walk(node.children)
        walk(self.children)
        return self

class DesignContext(BaseModel):
    form_version_id: str
    nodes: list[dict[str, Any]]
    component_catalog: list[dict[str, Any]]

class DesignRequest(BaseModel):
    contract: str
    request_id: str
    instruction: str = Field(min_length=1, max_length=8000)
    context: DesignContext

class DesignResponse(BaseModel):
    contract: str
    request_id: str
    result: FormSchema
    warnings: list[str] = Field(default_factory=list)

_model = os.environ.get("AI_MODEL", "openai:gpt-5.2")
_agent = Agent(
    _model,
    output_type=FormSchema,
    instructions=(
        "Design form schemas using only renderer_key values explicitly present "
        "in component_catalog. Return a nested tree with stable technical keys. "
        "Keep configuration small and consistent with each renderer. "
        "Use section nodes for hierarchy; display/action nodes have no values."
    ),
)

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/agents/form-design", response_model=DesignResponse)
async def form_design(req: DesignRequest):
    if req.contract != "form-design.v1":
        raise HTTPException(status_code=422, detail="Unsupported contract")
    allowed = {c["renderer_key"] for c in req.context.component_catalog if "renderer_key" in c}
    if not allowed:
        raise HTTPException(status_code=422, detail="No available form components")
    prompt = ("User request:\n" + req.instruction +
              "\nAvailable components and current FormNodes:\n" +
              req.context.model_dump_json(exclude_none=True))
    try:
        result = await _agent.run(prompt)
    except Exception:
        # Do not send model/provider details to the external caller.
        raise HTTPException(status_code=502, detail="Model inference failed") from None
    output = result.output
    def validate(children: dict[str, FormNode]):
        for key, node in children.items():
            if node.renderer_key not in allowed:
                raise HTTPException(status_code=422, detail=f"Unsupported component: {node.renderer_key}")
            if node.children and not next((c.get("can_have_children", False)
                    for c in req.context.component_catalog if c.get("renderer_key") == node.renderer_key), False):
                raise HTTPException(status_code=422, detail=f"Component cannot contain children: {key}")
            validate(node.children)
    validate(output.children)
    return DesignResponse(contract=req.contract, request_id=req.request_id, result=output)
