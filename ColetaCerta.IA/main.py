"""Inferência do Random Forest exportado pelo notebook ColetaCerta."""

from contextlib import asynccontextmanager
from datetime import date, timedelta
from pathlib import Path
from typing import Annotated
import hashlib
import math
import re
import unicodedata
import warnings

import joblib
import numpy as np
import pandas as pd
import sklearn
from fastapi import FastAPI, HTTPException, Query, Request
from pydantic import BaseModel, ConfigDict, Field
from sklearn.ensemble import RandomForestRegressor
from sklearn.exceptions import InconsistentVersionWarning


MODEL_PATH = Path(__file__).resolve().parent / "modelos" / "modelo_random_forest.joblib"
SKLEARN_VERSION = "1.6.1"
PREFIXO_REGIAO = "Prefeitura Operacional_"
COLUNAS_CALENDARIO = ["dia_semana", "fim_de_semana", "dia_mes", "feriado"]
# O notebook marcou exclusivamente esta data como feriado.
FERIADO_NOTEBOOK = date(2026, 9, 7)


def normalizar_regiao(valor: str) -> str:
    texto = unicodedata.normalize("NFKD", valor.strip().upper())
    texto = "".join(c for c in texto if not unicodedata.combining(c))
    return re.sub(r"[^A-Z0-9]+", " ", texto).strip()


class ModeloColetaCerta:
    def __init__(self, caminho: Path):
        if sklearn.__version__ != SKLEARN_VERSION:
            raise RuntimeError(
                f"Use scikit-learn {SKLEARN_VERSION}. "
                "Instale as dependências do requirements.txt em uma venv."
            )
        with warnings.catch_warnings():
            warnings.simplefilter("error", InconsistentVersionWarning)
            pacote = joblib.load(caminho)

        if not isinstance(pacote, dict) or not {"modelo", "colunas"} <= pacote.keys():
            raise RuntimeError("O arquivo precisa conter modelo e colunas.")
        self.modelo = pacote["modelo"]
        self.colunas = list(pacote["colunas"])
        if not isinstance(self.modelo, RandomForestRegressor):
            raise RuntimeError("Era esperado um RandomForestRegressor.")
        if self.colunas != list(self.modelo.feature_names_in_):
            raise RuntimeError("As colunas exportadas diferem das entradas do modelo.")
        self.regioes = [c.removeprefix(PREFIXO_REGIAO) for c in self.colunas
                        if c.startswith(PREFIXO_REGIAO)]
        colunas_esperadas = COLUNAS_CALENDARIO + [PREFIXO_REGIAO + r for r in self.regioes]
        if self.colunas != colunas_esperadas or len(self.regioes) != 32:
            raise RuntimeError("O esquema do modelo difere do notebook analisado.")
        self.mapa_regioes = {normalizar_regiao(r): r for r in self.regioes}
        self.sha256 = hashlib.sha256(caminho.read_bytes()).hexdigest()
        # Cada requisição trabalha com poucos registros e evita criar 100 workers.
        self.modelo.n_jobs = 1

    def resolver_regiao(self, regiao: str) -> str:
        chave = normalizar_regiao(regiao)
        if chave not in self.mapa_regioes:
            raise ValueError("Região não reconhecida. Consulte GET /regioes.")
        return self.mapa_regioes[chave]

    def montar_entradas(self, regiao: str, datas: list[date]) -> pd.DataFrame:
        entradas = pd.DataFrame(0, index=range(len(datas)), columns=self.colunas)
        entradas["dia_semana"] = [d.weekday() for d in datas]
        entradas["fim_de_semana"] = [int(d.weekday() >= 5) for d in datas]
        entradas["dia_mes"] = [d.day for d in datas]
        entradas["feriado"] = [int(d == FERIADO_NOTEBOOK) for d in datas]
        entradas[PREFIXO_REGIAO + self.resolver_regiao(regiao)] = 1
        return entradas

    def prever(self, regiao: str, datas: list[date]) -> list[float]:
        entradas = self.montar_entradas(regiao, datas)
        valores = np.asarray(self.modelo.predict(entradas), dtype=float).reshape(-1)
        if len(valores) != len(datas) or not np.isfinite(valores).all():
            raise RuntimeError("O modelo retornou valores inválidos.")
        if (valores < 0).any():
            raise RuntimeError("O modelo retornou uma previsão negativa.")
        return [round(float(v), 2) for v in valores]


@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.preditor = ModeloColetaCerta(MODEL_PATH)
    yield


app = FastAPI(
    title="ColetaCerta — Serviço de IA",
    version="1.0.0",
    description="Previsão de solicitações de Lixo e limpeza por subprefeitura, "
                "com o Random Forest treinado no notebook da equipe.",
    lifespan=lifespan,
)


class PrevisaoDiaria(BaseModel):
    data: date
    solicitacoesPrevistas: float


class PrevisaoResponse(BaseModel):
    regiao: str
    regiaoModelo: str
    periodoInicio: date
    periodoFim: date
    solicitacoesPrevistas: float
    modelo: str = "RandomForestRegressor"
    simulado: bool = False
    previsoesDiarias: list[PrevisaoDiaria]


class PreverRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    regiao: str = Field(min_length=1, max_length=100)
    data: date


def consultar_modelo(request: Request, regiao: str) -> tuple[ModeloColetaCerta, str]:
    preditor = request.app.state.preditor
    try:
        canonical = preditor.resolver_regiao(regiao)
    except ValueError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc
    return preditor, canonical


@app.get("/health", tags=["Serviço"])
def health(request: Request):
    return {"status": "ok", "modeloCarregado": True, "simulado": False,
            "scikitLearn": sklearn.__version__}


@app.get("/regioes", tags=["Modelo"])
def regioes(request: Request):
    return {"total": len(request.app.state.preditor.regioes),
            "regioes": request.app.state.preditor.regioes}


@app.get("/modelo", tags=["Modelo"])
def modelo(request: Request):
    preditor = request.app.state.preditor
    return {
        "algoritmo": "RandomForestRegressor",
        "arvores": preditor.modelo.n_estimators,
        "numeroEntradas": len(preditor.colunas),
        "colunas": preditor.colunas,
        "scikitLearn": sklearn.__version__,
        "sha256": preditor.sha256,
        "alvo": "Quantidade diária de solicitações SP156 do tema Lixo e limpeza",
        "treino": {"inicio": "2026-07-01", "fim": "2026-08-31"},
        "testeNotebook": {"inicio": "2026-09-01", "fim": "2026-09-30"},
        "calendario": {"diaSemana": "0=segunda, 6=domingo",
                       "feriado": "1 exclusivamente em 2026-09-07, conforme notebook"},
        "agregacao": "Soma de sete previsões diárias arredondadas a duas casas",
        "observacao": "Datas fora do período estudado usam padrões aprendidos "
                      "no trimestre. A previsão apoia o planejamento de equipes.",
    }


@app.get("/previsao", response_model=PrevisaoResponse, tags=["Previsões"])
def previsao(
    request: Request,
    regiao: Annotated[str, Query(min_length=1, max_length=100)],
    inicio: Annotated[date | None, Query(description="Primeiro dia do período. "
        "O padrão é a próxima segunda-feira após a data local do servidor.")] = None,
):
    preditor, canonical = consultar_modelo(request, regiao)
    if inicio is None:
        hoje = date.today()
        inicio = hoje + timedelta(days=7 - hoje.weekday())
    if inicio > date.max - timedelta(days=6):
        raise HTTPException(status_code=422, detail="A data inicial excede o limite.")
    datas = [inicio + timedelta(days=i) for i in range(7)]
    valores = preditor.prever(canonical, datas)
    return PrevisaoResponse(
        regiao=regiao.strip(),
        regiaoModelo=canonical,
        periodoInicio=inicio,
        periodoFim=datas[-1],
        solicitacoesPrevistas=round(math.fsum(valores), 2),
        previsoesDiarias=[PrevisaoDiaria(data=d, solicitacoesPrevistas=v)
                         for d, v in zip(datas, valores, strict=True)],
    )


@app.post("/prever", response_model=PrevisaoDiaria, tags=["Previsões"])
def prever(body: PreverRequest, request: Request):
    preditor, canonical = consultar_modelo(request, body.regiao)
    valor = preditor.prever(canonical, [body.data])[0]
    return PrevisaoDiaria(data=body.data, solicitacoesPrevistas=valor)
