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
