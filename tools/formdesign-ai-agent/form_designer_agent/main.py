"""Run with: uvicorn form_designer_agent.main:app --host 0.0.0.0 --port 9088"""
import os
from fastapi import FastAPI, HTTPException
from pydantic_ai.models.openai import OpenAIChatModel
from pydantic_ai.providers.openai import OpenAIProvider
from .tools.FormDesigner import (
    DesignRequest, DesignResponse, FormDesignerDeps,
    createAgent, validate_proposal,
)


def create_model() -> OpenAIChatModel:
    provider = OpenAIProvider(
        base_url=os.environ["AI_BASE_URL"],
        api_key=os.environ["AI_API_KEY"],
    )
    return OpenAIChatModel(os.environ.get("AI_MODEL", "gpt-5-nano"), provider=provider)


app = FastAPI(title="ZArchitecture Form Designer Agent")
agent = createAgent(create_model())


@app.post("/agents/form-design", response_model=DesignResponse)
@app.post("/form-designer-agent", response_model=DesignResponse)
async def run_form_designer_agent(request: DesignRequest) -> DesignResponse:
    if request.contract != "form-design.v1":
        raise HTTPException(status_code=422, detail="Unsupported contract")

    deps = FormDesignerDeps(request=request)
    prompt = (
        f"Pokyn uživatele: {request.instruction}\n"
        f"ID verze formuláře: {request.context.form_version_id}\n"
        "Nejprve zjisti dosavadní FormNodes a dostupné renderery pomocí nástrojů. "
        "Vrať úplný návrh FormSchema."
    )
    result = await agent.run(prompt, deps=deps)
    proposal = result.output
    errors = validate_proposal(proposal, request)
    if errors:
        raise HTTPException(status_code=422, detail={"validation_errors": errors})
    return DesignResponse(
        contract=request.contract,
        request_id=request.request_id,
        result=proposal,
        warnings=[],
    )
