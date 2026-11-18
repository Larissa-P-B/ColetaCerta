"""Testes com o arquivo real, incluindo equivalência com a preparação do notebook."""

from datetime import date, timedelta
from pathlib import Path
import sys
import unittest

import pandas as pd
from fastapi.testclient import TestClient

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from main import app


class TestServicoIA(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.client = TestClient(app)
        cls.client.__enter__()

    @classmethod
    def tearDownClass(cls):
        cls.client.__exit__(None, None, None)

    def test_health_modelo_real(self):
        response = self.client.get("/health")
        self.assertEqual(response.status_code, 200)
        self.assertFalse(response.json()["simulado"])
        self.assertEqual(response.json()["scikitLearn"], "1.6.1")

    def test_regioes_e_metadados(self):
        regioes = self.client.get("/regioes").json()
        modelo = self.client.get("/modelo").json()
        self.assertEqual(regioes["total"], 32)
        self.assertIn("IPIRANGA", regioes["regioes"])
        self.assertEqual(modelo["numeroEntradas"], 36)
        self.assertEqual(modelo["arvores"], 100)

    def test_diaria_equivale_ao_notebook(self):
        preditor = app.state.preditor
        referencia = pd.DataFrame({
            "Prefeitura Operacional": ["IPIRANGA"],
            "dia_semana": [0],
            "fim_de_semana": [0],
            "dia_mes": [7],
            "feriado": [1],
        })
        entrada = pd.get_dummies(referencia, columns=["Prefeitura Operacional"])
        entrada = entrada.reindex(columns=preditor.colunas, fill_value=0)
        esperado = round(float(preditor.modelo.predict(entrada)[0]), 2)
        response = self.client.post("/prever", json={"regiao": "Ipiranga", "data": "2026-09-07"})
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["solicitacoesPrevistas"], esperado)

    def test_calendario_reproduz_treinamento(self):
        datas = [date(2026, 9, 6), date(2026, 9, 7), date(2027, 9, 7)]
        entradas = app.state.preditor.montar_entradas("Ipiranga", datas)
        self.assertEqual(entradas["dia_semana"].tolist(), [6, 0, 1])
        self.assertEqual(entradas["fim_de_semana"].tolist(), [1, 0, 0])
        self.assertEqual(entradas["feriado"].tolist(), [0, 1, 0])
        regioes = entradas.filter(like="Prefeitura Operacional_")
        self.assertEqual(regioes.sum(axis=1).tolist(), [1, 1, 1])

    def test_semanal_soma_sete_dias(self):
        response = self.client.get("/previsao", params={"regiao": "Ipiranga", "inicio": "2026-09-28"})
        self.assertEqual(response.status_code, 200)
        body = response.json()
        self.assertEqual(body["periodoFim"], "2026-10-04")
        self.assertEqual(len(body["previsoesDiarias"]), 7)
        self.assertEqual(body["solicitacoesPrevistas"], round(sum(d["solicitacoesPrevistas"] for d in body["previsoesDiarias"]), 2))
        self.assertFalse(body["simulado"])
        self.assertEqual(body["modelo"], "RandomForestRegressor")

    def test_padrao_proxima_segunda(self):
        body = self.client.get("/previsao", params={"regiao": "Ipiranga"}).json()
        inicio = date.fromisoformat(body["periodoInicio"])
        self.assertEqual(inicio, date.today() + timedelta(days=7-date.today().weekday()))

    def test_nome_com_acento_preserva_contrato_csharp(self):
        response = self.client.get("/previsao", params={"regiao": "  Butantã  ", "inicio": "2026-09-07"})
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["regiao"], "Butantã")
        self.assertEqual(response.json()["regiaoModelo"], "BUTANTA")

    def test_todas_as_regioes(self):
        for regiao in app.state.preditor.regioes:
            with self.subTest(regiao=regiao):
                response = self.client.get("/previsao", params={"regiao": regiao, "inicio": "2026-09-07"})
                self.assertEqual(response.status_code, 200)
                self.assertGreaterEqual(response.json()["solicitacoesPrevistas"], 0)

    def test_entradas_invalidas(self):
        for params in [{}, {"regiao": " "}, {"regiao": "Atlantida"},
                       {"regiao": "x" * 101}, {"regiao": "Ipiranga", "inicio": "invalido"},
                       {"regiao": "Ipiranga", "inicio": "9999-12-31"}]:
            with self.subTest(params=params):
                self.assertEqual(self.client.get("/previsao", params=params).status_code, 422)

    def test_swagger_e_openapi(self):
        self.assertEqual(self.client.get("/docs").status_code, 200)
        paths = self.client.get("/openapi.json").json()["paths"]
        self.assertIn("/previsao", paths)
        self.assertIn("/prever", paths)


if __name__ == "__main__":
    unittest.main()
