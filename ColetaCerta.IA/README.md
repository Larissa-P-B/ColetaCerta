# ColetaCerta — serviço Python com Random Forest

Serviço FastAPI que carrega `modelos/modelo_random_forest.joblib` e prevê a
quantidade de solicitações SP156 do tema **Lixo e limpeza** por subprefeitura.
A previsão semanal soma sete previsões diárias do modelo recebido da equipe.

## Baixar e executar no Windows

Use Python 3.11 ou 3.12. Este serviço acompanha a API C# no repositório
[ColetaCerta](https://github.com/Larissa-P-B/ColetaCerta).
Ele não precisa do MySQL para funcionar isoladamente. A API C# utiliza MySQL.

Baixe o repositório em **Code → Download ZIP**, extraia e abra o PowerShell na
pasta `ColetaCerta.IA`, que contém `main.py`. Se preferir Git, execute:

```powershell
git clone https://github.com/Larissa-P-B/ColetaCerta.git
cd ColetaCerta\ColetaCerta.IA
```

Crie o ambiente virtual, instale as dependências e inicie o serviço.
Execute um comando por vez:

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m uvicorn main:app --host 127.0.0.1 --port 8000
```

Se tiver Python 3.11, use `py -3.11` no primeiro comando. Confira as instalações
disponíveis com `py -0p`. Mantenha o terminal aberto. Se o serviço simulado
anterior estiver usando a porta 8000, encerre-o com Ctrl+C antes desta execução.

O serviço carrega o modelo na inicialização. Os caminhos são resolvidos a partir
de `main.py`. O scikit-learn deve ser 1.6.1, versão registrada no artefato.

## Rotas

| Método | Rota | Função |
|---|---|---|
| GET | `/health` | Confirma o carregamento do modelo e a versão do scikit-learn |
| GET | `/regioes` | Lista as 32 subprefeituras aceitas |
| GET | `/modelo` | Mostra entradas, algoritmo, hash do arquivo e convenções |
| POST | `/prever` | Previsão diária a partir da região e da data |
| GET | `/previsao?regiao=Ipiranga` | Previsão de sete dias a partir da próxima segunda-feira |
| GET | `/previsao?regiao=Ipiranga&inicio=2026-11-23` | Previsão de sete dias a partir da data escolhida |

Swagger em http://localhost:8000/docs e saúde em http://localhost:8000/health.
O endereço `http://localhost:8000/` retorna `404 Not Found`, pois não existe
uma rota na raiz. Para visualizar e testar os endpoints, abra `/docs`.

### Linux ou macOS

Na pasta `ColetaCerta.IA`, com Python 3.11 ou 3.12 instalado:

```bash
python3 -m venv .venv
.venv/bin/python -m pip install -r requirements.txt
.venv/bin/python -m uvicorn main:app --host 127.0.0.1 --port 8000
```

Corpo de `POST /prever` para testar uma previsão diária.

```json
{
  "regiao": "Ipiranga",
  "data": "2026-11-23"
}
```

A resposta diária contém `data` e `solicitacoesPrevistas`. As estimativas podem
ter casas decimais porque são resultados de regressão. O serviço arredonda cada
previsão diária a duas casas e soma esses valores para obter o total semanal.

## Integração com o C# existente

### Roteiro de avaliação para o professor

1. Inicie o serviço Python com os comandos anteriores e mantenha o terminal aberto.
2. Acesse http://localhost:8000/health. O retorno esperado é HTTP `200`,
   com `modeloCarregado: true` e `simulado: false`.
3. Acesse http://localhost:8000/previsao?regiao=Ipiranga. O retorno esperado é
   HTTP `200`, com a previsão semanal calculada pelo modelo real.
4. Prepare o banco, os segredos de usuário e as migrações da API C# conforme
   o [README principal](../README.md). Execute `ColetaCerta.Api` no Visual Studio.
5. Confira a configuração `Integracoes:Ia:BaseUrl` abaixo.
6. Abra o Swagger C# em https://localhost:7189/swagger, ou na porta HTTPS
   exibida pelo Visual Studio.
7. Execute `POST /api/auth/login` com `login: admin` e `senha: Admin@12345`.
   Copie o campo `token` e cole somente o token em **Authorize**.
8. Execute `GET /api/integracoes/ia/previsao`, preenchendo `regiao` com `Ipiranga`.
   O resultado esperado é HTTP `200`, com região, período, quantidade prevista,
   modelo `RandomForestRegressor` e `consultadoEm`.

As credenciais são de demonstração local e são criadas pelas migrações da API C#.
Também é possível consultar com `user` / `User@12345`.
Mantenha as duas aplicações executando ao mesmo tempo. O serviço IoT não é
necessário para testar a rota de IA.

### URL utilizada pelo C#

Mantenha a configuração Wokwi e atualize a URL da IA na seção `Integracoes` de
`ColetaCerta.Api/appsettings.json`.

```json
"Integracoes": {
  "Wokwi": {
    "BaseUrl": "http://localhost:8180/"
  },
  "Ia": {
    "BaseUrl": "http://localhost:8000/"
  }
}
```

Com os serviços no mesmo computador, execute Python e C#. Autorize com JWT no
Swagger da ColetaCerta e consulte `GET /api/integracoes/ia/previsao` com região
`Ipiranga`. A rota e os DTOs C# existentes recebem os campos abaixo.

- `regiao`, preservando o nome solicitado com espaços externos removidos
- `periodoInicio` e `periodoFim`, em formato ISO
- `solicitacoesPrevistas`, total do período
- `modelo`, com o valor `RandomForestRegressor`

O Python também fornece `regiaoModelo`, `simulado: false` e `previsoesDiarias`.
O DTO atual do C# apresenta seus campos mapeados e acrescenta `consultadoEm`.
Para consultar uma data inicial específica, use diretamente o Swagger Python.
O endpoint C# atual utiliza o período padrão porque encaminha somente a região.

O valor padrão do período foi definido neste serviço como **a próxima segunda-feira
após a data local do servidor**, inclusive quando a consulta ocorre numa segunda.
O notebook não especificava essa escolha operacional. O parâmetro `inicio`
permite selecionar outro início, em qualquer dia da semana.

`localhost` aponta para o computador que executa o C#. Para executar em outra
máquina ou VM, ajuste o endereço e a configuração de rede. O serviço Python de
desenvolvimento atende em 127.0.0.1, porta 8000.

### Problemas comuns

| Situação | Como resolver |
|---|---|
| C# retorna `401` | Faça login novamente e autorize com o token no Swagger C# |
| C# retorna `503` | Confirme que o Python está executando e que a URL base é `http://localhost:8000/` |
| C# retorna `502` | Teste a região em `/regioes` e confira a resposta e o terminal do Python |
| C# retorna `504` | Confira o terminal do Python; a API C# possui limite de tempo para a chamada |
| Porta 8000 ocupada | Encerre o serviço anterior com Ctrl+C ou ajuste a porta e a URL configurada no C# |
| Erro ao carregar o modelo | Confirme a existência de `modelos/modelo_random_forest.joblib` e reinstale as dependências do arquivo `requirements.txt` na venv |
| `No module named fastapi` | Instale as dependências e execute com o mesmo Python da `.venv`, conforme os comandos acima |

## Preparação reproduzida do notebook

O arquivo recebido contém `modelo` e `colunas`. O serviço respeita a ordem das
36 colunas exportadas e confirma que ela coincide com `feature_names_in_`.

| Entrada | Preparação |
|---|---|
| `dia_semana` | Segunda-feira = 0 até domingo = 6 |
| `fim_de_semana` | 1 no sábado/domingo e 0 nos outros dias |
| `dia_mes` | Dia do mês, entre 1 e 31 |
| `feriado` | 1 exclusivamente em 07/09/2026, reproduzindo o notebook |
| 32 colunas de subprefeitura | One-hot, com 1 na região escolhida e 0 nas demais |

A comparação de nomes aceita diferenças de maiúsculas, acentos e separadores.
Por exemplo, `Butantã` é associado à coluna de `BUTANTA`. Consulte `/regioes`
para os nomes das subprefeituras. Os nomes representam regiões administrativas.

## Alcance e resultados registrados no notebook

O notebook informa treino de julho a agosto de 2026 e teste em setembro de 2026.
Ele registra MAE diário de 6,619 e R² de 0,439 no teste. O baseline de média por
região/dia teve MAE de 6,378. Para agregações semanais, registra MAE de 20,094 e
R² de 0,74. Essas métricas foram lidas do notebook e não recalculadas aqui.
O CSV de treinamento não acompanha este serviço.

As previsões representam solicitações dos cidadãos e apoiam o planejamento de
equipes. Datas futuras reutilizam os padrões de calendário aprendidos no
trimestre. O indicador de feriado reproduz a data única do notebook, com valor
zero nas demais datas, incluindo outros feriados. Essa convenção requer revisão
pela pessoa de IA se o modelo for evoluído para um calendário mais amplo.

## Testes automatizados

```powershell
.\.venv\Scripts\python.exe -m pip install -r requirements-test.txt
.\.venv\Scripts\python.exe -m unittest discover -s tests -v
```

Os dez testes usam o modelo real e cobrem equivalência com a codificação do
notebook, soma semanal, nomes com acentos, as 32 regiões, entradas inválidas e
documentação OpenAPI. Esse conjunto é adicional aos testes xUnit da API C#.

## Arquivos

- `main.py` contém o carregamento, a preparação e os endpoints.
- `modelos/modelo_random_forest.joblib` contém o artefato fornecido pela equipe.
- `requirements.txt` fixa as versões usadas para validar este serviço.
- `requirements-test.txt` inclui as dependências de teste.
- `tests/test_api.py` verifica a integração com o modelo real.

Carregue apenas modelos de origem confiável. O formato joblib pode executar
código durante a desserialização.
