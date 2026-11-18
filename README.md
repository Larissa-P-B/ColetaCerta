# Integrantes: 
| Nome | RM |
|---|---|
|Dayana Ticona | 558023 |
|Larissa Biusse | 564068 |
|Rodrigo Leme | 550266 |
|Thamiris Almeida | 559155 |
|Werbeth Nunes | 559067 |

# ColetaCerta

API REST para gerenciar pontos de coleta e suas lixeiras. A API consulta o ESP32 simulado no Wokwi para obter o nível de ocupação e um serviço Python para obter previsões de solicitações de coleta por região.

Projeto da Global Solution CitizenTech para **C# Software Development** e **Arquitetura Orientada a Serviços (SOA) e Web Services**.

## Tecnologias e organização

- C# com ASP.NET Core Web API e .NET 10.
- MySQL 8.0, Entity Framework Core 10 e migrações. Ambiente validado com MySQL 8.0.46.
- JWT com os perfis `ADMIN` e `USER`. Senhas armazenadas como hash pelo `PasswordHasher` do ASP.NET Core.
- Swagger UI com a especificação OpenAPI gerada pelo ASP.NET Core.
- Consultas com LINQ e operações assíncronas de banco e HTTP.
- xUnit para testes de regras, serviços, rotas e autorização.

| Projeto ou pasta | Responsabilidade |
|---|---|
| `ColetaCerta.Api/Controllers` | Rotas REST, respostas HTTP e autorização |
| `ColetaCerta.Api/DTOs` | Contratos de entrada e saída |
| `ColetaCerta.Api/Entities` | Entidades e regras de domínio |
| `ColetaCerta.Api/Interfaces` e `Services` | Contratos e implementações injetadas nos controllers |
| `ColetaCerta.Api/Data` e `Migrations` | Mapeamento do EF Core e evolução do banco |
| `ColetaCerta.Tests` | Testes automatizados |
| `ColetaCerta.GeradorSenhas` | Utilitário de desenvolvimento para gerar hashes das contas de demonstração e a chave JWT |

Um ponto de coleta pode ter várias lixeiras. Cada lixeira pertence a um ponto e tem um código único. A exclusão de um ponto com lixeiras vinculadas retorna `409 Conflict`. A ocupação é calculada a partir da leitura consultada no ESP32.

## Como executar

### 1. Prepare o ambiente

Instale o SDK do .NET 10, o Visual Studio 2026 com a carga de trabalho **Desenvolvimento para ASP.NET e Web** e o MySQL Server 8.0 ou superior. Abra a solução `ColetaCerta` e selecione `ColetaCerta.Api` como projeto de inicialização.

No MySQL Workbench ou no cliente MySQL, crie o banco de desenvolvimento.

```sql
CREATE DATABASE IF NOT EXISTS coletacerta
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
```

Use uma conta do MySQL com acesso ao banco e permissão para aplicar as migrações.

### 2. Configure a conexão e o JWT

No Visual Studio, clique com o botão direito em `ColetaCerta.Api` e selecione **Gerenciar Segredos do Usuário**. Preencha os valores conforme seu ambiente.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=coletacerta;User=SEU_USUARIO;Password=SUA_SENHA;"
  },
  "Jwt": {
    "Key": "SUBSTITUA_POR_UMA_CHAVE_ALEATORIA"
  }
}
```

Gere a chave com 32 bytes aleatórios. Em PowerShell, o comando abaixo gera um valor em Base64 para colar em `Jwt:Key`.

```powershell
$chaveColetaCerta = New-Object byte[] 32
$geradorColetaCerta = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$geradorColetaCerta.GetBytes($chaveColetaCerta)
$geradorColetaCerta.Dispose()
[Convert]::ToBase64String($chaveColetaCerta)
```

Mantenha a senha do banco e a chave JWT fora do Git. Em ambientes configurados por variáveis de ambiente, use `ConnectionStrings__DefaultConnection` e `Jwt__Key`.

No `appsettings.json`, confira o emissor, o público e a validade do token.

```json
{
  "Jwt": {
    "Issuer": "ColetaCerta.Api",
    "Audience": "ColetaCerta.Clientes",
    "ExpirationMinutes": 120
  }
}
```

### 3. Aplique as migrações

No Visual Studio, abra **Ferramentas → Gerenciador de Pacotes NuGet → Console do Gerenciador de Pacotes**. Selecione `ColetaCerta.Api` como projeto padrão e execute com a configuração de desenvolvimento ativa.

```powershell
Update-Database
```

As migrações criam as tabelas `pontos_coleta`, `lixeiras` e `usuarios` e inserem as contas de demonstração.

### 4. Abra o Swagger

Execute o perfil HTTPS da API no Visual Studio com **Ctrl + F5**. No ambiente `Development`, abra `https://localhost:<porta>/swagger` usando a porta exibida na execução. `https://localhost:7189/swagger`

## Autenticação e perfis

| Login | Senha de demonstração | Perfil | Acesso |
|---|---|---|---|
| `admin` | `Admin@12345` | `ADMIN` | Consulta, cadastro, atualização e exclusão |
| `user` | `User@12345` | `USER` | Consulta |

Use essas credenciais somente no ambiente local de demonstração. As migrações armazenam os hashes das senhas.

1. No Swagger, execute `POST /api/auth/login` com o corpo abaixo.
2. Copie o valor de `token` retornado no JSON.
3. Clique em **Authorize** e cole somente o token. Para chamadas fora do Swagger, envie o cabeçalho `Authorization: Bearer <token>`.

```json
{
  "login": "admin",
  "senha": "Admin@12345"
}
```

## Endpoints

| Método | Rota | Acesso | Sucesso |
|---|---|---|---|
| POST | `/api/auth/login` | Público | `200` |
| GET | `/api/health/banco` | Autenticado | `200` |
| GET | `/api/pontos-coleta` | ADMIN, USER | `200` |
| GET | `/api/pontos-coleta/{id}` | ADMIN, USER | `200` |
| POST | `/api/pontos-coleta` | ADMIN | `201` |
| PUT | `/api/pontos-coleta/{id}` | ADMIN | `204` |
| DELETE | `/api/pontos-coleta/{id}` | ADMIN | `204` |
| GET | `/api/lixeiras` | ADMIN, USER | `200` |
| GET | `/api/lixeiras/{id}` | ADMIN, USER | `200` |
| POST | `/api/lixeiras` | ADMIN | `201` |
| PUT | `/api/lixeiras/{id}` | ADMIN | `204` |
| DELETE | `/api/lixeiras/{id}` | ADMIN | `204` |
| GET | `/api/integracoes/iot/lixeiras/{id}` | ADMIN, USER | `200` |
| GET | `/api/integracoes/ia/previsao?regiao=Ipiranga` | ADMIN, USER | `200` |

Para cadastrar um ponto de coleta, envie o JSON abaixo a `POST /api/pontos-coleta` com o token de administrador.

```json
{
  "nome": "Ponto de Coleta Comunitário",
  "endereco": "Rua Exemplo, 100",
  "bairro": "Vila Brasílio Machado",
  "cidade": "São Paulo"
}
```

Use o ID retornado como `pontoColetaId` no cadastro da lixeira. No exemplo, o ponto tem ID `1`.

```json
{
  "codigo": "LIX-001",
  "capacidadeLitros": 120,
  "pontoColetaId": 1
}
```

## Integrações com IoT e IA

Configure os endereços no `appsettings.json` ou por variáveis de ambiente. Os endereços abaixo são os valores locais usados durante o desenvolvimento. Cada URL base deve terminar com `/`.

```json
{
  "Integracoes": {
    "Wokwi": {
      "BaseUrl": "http://localhost:8180/"
    },
    "Ia": {
      "BaseUrl": "http://localhost:8000/"
    }
  }
}
```

| Integração | Variável de ambiente | Rota consultada pela API |
|---|---|---|
| ESP32 | `Integracoes__Wokwi__BaseUrl` | `GET /leitura` |
| Serviço Python | `Integracoes__Ia__BaseUrl` | `GET /previsao?regiao=Ipiranga` |

### ESP32

O endereço do Wokwi deve apontar para o servidor HTTP do ESP32, acessível à API pelo encaminhamento de porta ou pelo endereço combinado com a equipe. O ID devolvido pelo dispositivo deve corresponder ao da lixeira cadastrada e solicitada na API.

```json
{
  "lixeiraId": 1,
  "ocupacaoPercentual": 85
}
```

| Percentual | Situação calculada pela API |
|---|---|
| De 0% a 10% | `Vazia` |
| Acima de 10% e abaixo de 80% | `ParcialmenteCheia` |
| De 80% a menos de 95% | `QuaseCheia` |
| De 95% a 100% | `Cheia` |

### Serviço de IA

O serviço Python executa o modelo. A ColetaCerta consulta e valida a previsão antes de devolvê-la ao cliente. Exemplo ilustrativo do contrato HTTP.

```json
{
  "regiao": "Ipiranga",
  "periodoInicio": "2026-11-16",
  "periodoFim": "2026-11-22",
  "solicitacoesPrevistas": 28,
  "modelo": "RandomForest"
}
```

| Retorno | Significado nas integrações |
|---|---|
| `400 Bad Request` | Região ausente ou inválida na consulta à IA |
| `404 Not Found` | Lixeira solicitada fora do cadastro |
| `502 Bad Gateway` | O serviço externo retornou erro HTTP ou uma resposta inválida |
| `503 Service Unavailable` | A API não conseguiu se conectar ao serviço externo |
| `504 Gateway Timeout` | O serviço externo excedeu o tempo de resposta |

## Testes e estado da entrega

Na raiz da solução, execute:

```powershell
dotnet test
```

No Visual Studio, também é possível usar **Teste → Gerenciador de Testes → Executar todos**.

| Validação | Resultado |
|---|---|
| Testes automatizados | **22 aprovados** |
| Logins `admin` e `user` criados pela migração | Validados no MySQL |
| Cadastro de ponto com token de `ADMIN` | `201` no teste automatizado |
| Tentativa de cadastro com `USER` | `403`, com consulta permitida |
| Login com senha incorreta e consulta IoT sem token válido | `401` nos testes automatizados |
| Ocupação, leitura de outra lixeira e respostas de IA | Validadas com respostas simuladas |
| Serviços externos desligados | `503` observado no Swagger nas duas rotas |
| Consulta ponta a ponta ao ESP32 e ao serviço Python | Pendente até a disponibilização dos serviços pela equipe |

## Resumo das evidências

| Evidência no Swagger | Resultado esperado |
|---|---|
| Login como ADMIN | **200**, com perfil `ADMIN` |
| Cadastro de ponto de coleta | **201** |
| Cadastro de lixeira vinculada ao ponto | **201** |
| Consulta dos registros cadastrados | **200**, mostrando os dados |
| Atualização e consulta do registro alterado | **204** na atualização e **200** na consulta |
| Exclusão de um registro de teste | **204** |
| Cadastro com capacidade inválida | **400** |
| Cadastro com código de lixeira duplicado | **409** |
| Exclusão de ponto com lixeiras vinculadas | **409** |
| USER consultando e tentando cadastrar | **200** na consulta e **403** no cadastro |
| Consulta sem token | **401** |

## 01. Login como ADMIN

**Endpoint:** `POST /api/auth/login`

**Resultado esperado:** **200 OK**, com o campo `perfil` igual a `ADMIN`.

**Print — `01-login-admin.png`**

<img width="1192" height="646" alt="image" src="https://github.com/user-attachments/assets/de066f56-173f-421e-8cab-cd4f91f4d474" />


## 02. Cadastro de ponto de coleta

**Endpoint:** `POST /api/pontos-coleta`

**Resultado esperado:** **201 Created**, com o ID do ponto cadastrado.

**Print — `02-cadastro-ponto.png`**

<img width="1191" height="842" alt="image" src="https://github.com/user-attachments/assets/0f539989-48ca-4269-9bc7-9be74969a9e2" />


## 03. Cadastro de lixeira vinculada ao ponto

**Endpoint:** `POST /api/lixeiras`

**Resultado esperado:** **201 Created**, com o campo `pontoColetaId` correspondente ao ponto existente.

**Print — `03-cadastro-lixeira.png`**

<img width="1190" height="817" alt="image" src="https://github.com/user-attachments/assets/44b2ed07-5b25-4d42-b3a7-20e08849593a" />


## 04. Consulta dos registros cadastrados

**Endpoint:** `GET /api/pontos-coleta` e `GET /api/lixeiras`

**Resultado esperado:** **200 OK**, exibindo os registros cadastrados.

**Print — `04a-consulta-pontos.png`**

<img width="1188" height="780" alt="image" src="https://github.com/user-attachments/assets/7ce73b4a-58f3-49a7-aea4-4c8f67345221" />


**Print — `04b-consulta-lixeiras.png`**

<img width="1186" height="746" alt="image" src="https://github.com/user-attachments/assets/0391d096-07bf-4d2b-906c-d6ba20b37703" />


## 05. Atualização e consulta do registro alterado

**Endpoint:** `PUT /api/lixeiras/{id}` e `GET /api/lixeiras/{id}`

**Resultado esperado:** **204 No Content** na atualização e **200 OK** na consulta, com o valor alterado visível.

**Print — `05a-atualizacao-lixeira.png`**

<img width="1183" height="755" alt="image" src="https://github.com/user-attachments/assets/7ee67c84-bb8b-43b2-bd52-80f5683e5830" />


**Print — `05b-consulta-lixeira-atualizada.png`**

<img width="1177" height="782" alt="image" src="https://github.com/user-attachments/assets/0f7f0e32-4dd8-4486-a205-179e212e9c0c" />


## 06. Exclusão de um registro de teste

**Endpoint:** `DELETE /api/lixeiras/{id}`

**Resultado esperado:** **204 No Content** para uma lixeira de teste existente.

**Print — `06-exclusao-lixeira.png`**

<img width="1182" height="826" alt="image" src="https://github.com/user-attachments/assets/f89763ec-0337-43b5-ac3f-8ad6faae9017" />


## 07. Cadastro com capacidade inválida

**Endpoint:** `POST /api/lixeiras`

**Resultado esperado:** **400 Bad Request** ao enviar `capacidadeLitros` igual a `0`, com os demais campos válidos.

**Print — `07-capacidade-invalida.png`**

<img width="1418" height="723" alt="image" src="https://github.com/user-attachments/assets/d9ce07a1-a77c-43a7-b2e2-66f155151529" />


## 08. Cadastro com código de lixeira duplicado

**Endpoint:** `POST /api/lixeiras`

**Resultado esperado:** **409 Conflict** ao enviar o código de uma lixeira ainda cadastrada, com os demais campos válidos.

**Print — `08-codigo-duplicado.png`**

<img width="1417" height="698" alt="image" src="https://github.com/user-attachments/assets/6bb11415-bf2f-4e70-9ce6-aef772952262" />


## 09. Exclusão de ponto com lixeiras vinculadas

**Endpoint:** `DELETE /api/pontos-coleta/{id}`

**Resultado esperado:** **409 Conflict** ao tentar excluir um ponto que possui pelo menos uma lixeira vinculada.

**Print — `09-ponto-com-lixeiras.png`**

<img width="1418" height="626" alt="image" src="https://github.com/user-attachments/assets/c87bcc0c-570e-4056-a3e9-3b88a8ebab6e" />


## 10. USER consultando e tentando cadastrar

**Endpoint:** `GET /api/pontos-coleta` e `POST /api/pontos-coleta`

**Resultado esperado:** Com o token de `USER`, **200 OK** na consulta e **403 Forbidden** no cadastro com dados válidos.

**Print — `10a-user-consulta.png`**
<img width="1417" height="677" alt="image" src="https://github.com/user-attachments/assets/55e7630f-de3e-4cc0-a70a-ef25c6ee81cf" />


<img width="1392" height="748" alt="image" src="https://github.com/user-attachments/assets/dc7d13e4-a233-4dc0-b143-8c0fc7f15249" />


**Print — `10b-user-cadastro-bloqueado.png`**

<img width="1426" height="637" alt="image" src="https://github.com/user-attachments/assets/68a01d0e-9c90-40cf-9f2f-4bd8563cf086" />


## 11. Consulta sem token

**Endpoint:** `GET /api/pontos-coleta`

**Resultado esperado:** **401 Unauthorized** após remover a autorização no botão Authorize do Swagger.

**Print — `11-consulta-sem-token.png`**

<img width="1413" height="517" alt="image" src="https://github.com/user-attachments/assets/1cb0ef48-b408-4c3a-aa40-a815c955c564" />


