# Desafio Arquitetura de Software

Projeto desenvolvido para o desafio de arquitetura de software, com foco no registro de movimentações financeiras e consulta do saldo consolidado de uma conta.

## Repositório

https://github.com/AlanSiqma/desafio-arquitetura-software

## Arquitetura

A solução é composta por dois microserviços:

- `DesafioArquiteturaSoftware.Api`
  - Responsável pelas operações de escrita.
  - Criação de movimentações financeiras.
  - Atualização de movimentações.
  - Exclusão lógica de movimentações.
  - Controle de idempotência.
  - Controle de concorrência.

- `DesafioArquiteturaSoftware.Query`
  - Responsável exclusivamente pelas consultas.
  - Consulta do saldo consolidado de uma conta.
  - Consulta do saldo em um determinado momento.
  - Utiliza Dapper para acesso ao banco de leitura.

A persistência utiliza PostgreSQL com replicação de leitura.

Fluxo simplificado:

```text
                         +------------------+
                         |     Cliente      |
                         +--------+---------+
                                  |
                    +-------------+-------------+
                    |                           |
                    v                           v
          +------------------+        +------------------+
          |    Write API     |        |    Query API     |
          |                  |        |                  |
          | EF Core          |        | Dapper           |
          +--------+---------+        +--------+---------+
                   |                           |
                   v                           v
          +------------------+        +------------------+
          | PostgreSQL       |        | PostgreSQL       |
          | Primary          |------->| Read Replica     |
          +------------------+        +------------------+
                    Streaming Replication
```

## Tecnologias

- C#
- .NET
- ASP.NET Core Minimal API
- Entity Framework Core
- Dapper
- PostgreSQL
- Npgsql
- Docker
- Docker Compose
- xUnit
- Moq
- Postman

## Pré-requisitos

Para executar o projeto localmente, é necessário ter instalado:

- .NET SDK
- Docker Desktop
- Git
- Postman (opcional, para testes manuais)

## Clonar o projeto

```bash
git clone https://github.com/AlanSiqma/desafio-arquitetura-software.git
cd desafio-arquitetura-software
```

## 1. Subir a infraestrutura com Docker Compose

Na raiz do projeto, execute:

```bash
docker compose up -d
```

Verifique os containers:

```bash
docker compose ps
```

Para visualizar os logs:

```bash
docker compose logs
```

Para acompanhar os logs em tempo real:

```bash
docker compose logs -f
```

A infraestrutura deve permanecer em execução enquanto as APIs estiverem sendo executadas localmente.

Para parar a infraestrutura:

```bash
docker compose down
```

Para recriar os containers:

```bash
docker compose down
docker compose up -d
```

## 2. Executar a Write API

Abra um terminal na raiz do projeto e execute:

```bash
dotnet run --project src/DesafioArquiteturaSoftware.Api
```

A Write API é responsável pelas operações de escrita.

Endpoints:

```text
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
```

### Criar uma transação

A criação exige o header de idempotência:

```http
Idempotency-Key: minha-chave-123
Content-Type: application/json
```

Body:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "description": "Deposito inicial",
  "amount": 1000.00,
  "type": 1
}
```

Tipos de movimentação:

```text
1 = Credit
2 = Debit
```

### Atualizar uma transação

```http
PUT /api/transactions/{id}
Content-Type: application/json
```

Body:

```json
{
  "description": "Pagamento atualizado",
  "amount": 200.00,
  "type": 2
}
```

### Excluir uma transação

```http
DELETE /api/transactions/{id}
```

A exclusão é lógica, preservando o registro no banco de dados.

## 3. Executar a Query API

Em outro terminal:

```bash
dotnet run --project src/DesafioArquiteturaSoftware.Query
```

A Query API é executada separadamente da Write API.

Ela utiliza a conexão configurada em:

```text
src/DesafioArquiteturaSoftware.Query/appsettings.json
```

A connection string utilizada pela Query API deve apontar para o banco de leitura (`ReadDatabase`).

### Consultar o saldo atual

Endpoint:

```http
GET /api/accounts/{accountId}/balance
```

Exemplo:

```http
GET /api/accounts/00000000-0000-0000-0000-000000000001/balance
```

Exemplo de resposta:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "balance": 3750.00,
  "asOf": "2026-09-29T19:49:14.47038Z"
}
```

### Consultar o saldo em determinado momento

Também é possível informar o parâmetro `at`:

```http
GET /api/accounts/{accountId}/balance?at=2026-09-29T19:00:00Z
```

Exemplo:

```http
GET /api/accounts/00000000-0000-0000-0000-000000000001/balance?at=2026-09-29T19:00:00Z
```

## 4. Fluxo completo para teste

Para validar o ambiente localmente, siga esta ordem.

### Passo 1 — Subir o PostgreSQL

Na raiz do projeto:

```bash
docker compose up -d
```

Verifique:

```bash
docker compose ps
```

### Passo 2 — Iniciar a Write API

Em um terminal:

```bash
dotnet run --project src/DesafioArquiteturaSoftware.Api
```

### Passo 3 — Criar uma movimentação de crédito

```http
POST /api/transactions
```

Header:

```text
Idempotency-Key: credito-001
```

Body:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "description": "Deposito inicial",
  "amount": 1000.00,
  "type": 1
}
```

### Passo 4 — Criar uma movimentação de débito

```http
POST /api/transactions
```

Header:

```text
Idempotency-Key: debito-001
```

Body:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "description": "Pagamento",
  "amount": 250.00,
  "type": 2
}
```

### Passo 5 — Iniciar a Query API

Em outro terminal:

```bash
dotnet run --project src/DesafioArquiteturaSoftware.Query
```

### Passo 6 — Consultar o saldo

```http
GET /api/accounts/00000000-0000-0000-0000-000000000001/balance
```

O saldo é calculado considerando:

```text
Crédito → + valor
Débito  → - valor
```

## 5. Testes automatizados

Para executar todos os testes:

```bash
dotnet test
```

Para executar a solução com build:

```bash
dotnet build
dotnet test
```

Os testes estão organizados entre os componentes de escrita e consulta.

Estrutura dos testes:

```text
src/
├── DesafioArquiteturaSoftware.Tests/
└── DesafioArquiteturaSoftware.Query.Tests/
```

## 6. Postman

Uma collection do Postman está disponível para facilitar os testes manuais das APIs.

Arquivo:

```text
DesafioArquiteturaSoftware.postman_collection.json
```

A collection contém:

### Write API

```text
Create Transaction - Credit
Create Transaction - Debit
Update Transaction
Delete Transaction
```

### Query API

```text
Get Current Account Balance
Get Account Balance At
```

Variáveis utilizadas:

```text
writeUrl
queryUrl
accountId
transactionId
```

Após importar a collection no Postman, ajuste as URLs caso as APIs estejam sendo executadas em portas diferentes das configuradas na collection.

## 7. Estrutura do projeto

```text
src/
├── DesafioArquiteturaSoftware.Api/
├── DesafioArquiteturaSoftware.Application/
├── DesafioArquiteturaSoftware.Domain/
├── DesafioArquiteturaSoftware.Infrastructure/
├── DesafioArquiteturaSoftware.Tests/
├── DesafioArquiteturaSoftware.Query/
└── DesafioArquiteturaSoftware.Query.Tests/
```

### Write API

A camada de escrita segue o fluxo:

```text
Api
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
 ↓
PostgreSQL Primary
```

### Query API

A camada de consulta segue o fluxo:

```text
Query API
 ↓
Queries
 ↓
Repository
 ↓
Dapper
 ↓
PostgreSQL Read Replica
```

A Query API não possui dependência dos projetos da Write API.

## 8. Idempotência

A criação de movimentações utiliza uma chave de idempotência enviada através do header:

```text
Idempotency-Key
```

O objetivo é evitar a criação duplicada de uma mesma operação quando uma requisição é repetida devido a retry, timeout ou instabilidade de comunicação.

A chave e o hash da requisição são persistidos junto com a movimentação dentro da mesma transação.

Uma nova requisição utilizando a mesma chave:

- Com os mesmos dados, retorna o recurso originalmente criado.
- Com dados diferentes, retorna conflito.

## 9. Concorrência

As operações de atualização utilizam controle de concorrência otimista.

A entidade possui uma propriedade interna de versão:

```text
Version
```

A versão não faz parte do contrato público da API.

Quando ocorre uma alteração concorrente detectada pelo mecanismo de persistência, a aplicação retorna conflito (`409`).

## 10. Tratamento de falhas

As operações de escrita utilizam transações para manter a consistência entre:

- Movimentação financeira.
- Registro de idempotência.

Em caso de falha durante a operação:

```text
Rollback
   ↓
Limpeza do tracking
   ↓
Exceção tratada pela API
```

A API também possui tratamento global de exceções para converter erros de domínio e infraestrutura em respostas HTTP apropriadas.

## 11. PostgreSQL e replicação

O ambiente local utiliza PostgreSQL com uma instância principal e uma instância destinada à leitura.

Fluxo de persistência:

```text
Write API
    |
    v
PostgreSQL Primary
    |
    | Streaming Replication
    v
PostgreSQL Read Replica
    |
    v
Query API
```

A Write API grava no banco principal.

A Query API realiza as consultas no banco de leitura.

Essa separação permite que as operações de consulta sejam escaladas independentemente das operações de escrita.

## 12. Parando o ambiente

Para parar os containers:

```bash
docker compose down
```

Para remover também os volumes persistentes:

```bash
docker compose down -v
```

> Atenção: `docker compose down -v` remove os volumes associados ao ambiente e os dados persistidos nesses volumes.

## 13. Resumo dos comandos

```bash
# Clonar o projeto
git clone https://github.com/AlanSiqma/desafio-arquitetura-software.git

# Entrar no diretório
cd desafio-arquitetura-software

# Subir infraestrutura
docker compose up -d

# Verificar containers
docker compose ps

# Terminal 1 - Write API
dotnet run --project src/DesafioArquiteturaSoftware.Api

# Terminal 2 - Query API
dotnet run --project src/DesafioArquiteturaSoftware.Query

# Executar testes
dotnet test

# Parar infraestrutura
docker compose down
```

## 14. Licença

Projeto desenvolvido para fins de avaliação técnica no desafio de arquitetura de software.
