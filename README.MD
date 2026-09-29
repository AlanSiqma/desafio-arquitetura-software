# Desafio Arquitetura de Software

Projeto desenvolvido para o desafio de arquitetura de software.

O objetivo deste README é explicar como executar o projeto localmente e realizar os testes. As decisões arquiteturais e seus respectivos trade-offs estão documentados em `docs/adr`.

## Repositório

https://github.com/AlanSiqma/desafio-arquitetura-software

## Pré-requisitos

Instale:

- .NET SDK
- Docker Desktop
- Git
- Postman (opcional)

Confirme as instalações:

```powershell
dotnet --version
docker --version
docker compose version
git --version
```

## Clonar o projeto

```powershell
git clone https://github.com/AlanSiqma/desafio-arquitetura-software.git
cd desafio-arquitetura-software
```

## 1. Subir o PostgreSQL

Na raiz do projeto, execute:

```powershell
docker compose up -d
```

Verifique se os containers estão em execução:

```powershell
docker compose ps
```

Para acompanhar os logs:

```powershell
docker compose logs -f
```

Para consultar os logs de um serviço específico:

```powershell
docker compose logs -f <nome-do-servico>
```

A infraestrutura deve estar em execução antes de iniciar as APIs.

### Parar o PostgreSQL

```powershell
docker compose down
```

Para remover também os volumes:

```powershell
docker compose down -v
```

> `docker compose down -v` remove os dados persistidos nos volumes do ambiente local.

## 2. Configuração da Write API

A Write API utiliza o PostgreSQL Primary.

A configuração da conexão com o banco está no arquivo de configuração da aplicação:

```text
src/DesafioArquiteturaSoftware.Api/appsettings.json
```

Antes de executar a aplicação, confirme que a connection string aponta para o PostgreSQL Primary iniciado pelo Docker Compose.

As migrations do Entity Framework Core são aplicadas pela aplicação durante a inicialização.

## 3. Executar a Write API

Abra um terminal na raiz do projeto:

```powershell
dotnet run --project .\src\DesafioArquiteturaSoftware.Api
```

A porta utilizada será a configurada no projeto, normalmente apresentada no terminal ao iniciar a aplicação.

Exemplo:

```text
Now listening on: https://localhost:XXXX
Now listening on: http://localhost:XXXX
```

Utilize a URL apresentada pelo terminal para realizar as requisições.

### Endpoints da Write API

```text
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
```

### Criar uma transação

Request:

```http
POST /api/transactions
Content-Type: application/json
Idempotency-Key: <chave-unica>
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

Tipos:

```text
1 = Credit
2 = Debit
```

A resposta da criação contém o identificador da movimentação criada.

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

A operação é realizada através de exclusão lógica.

## 4. Configuração da Query API

A Query API utiliza o PostgreSQL Read Replica.

A configuração está em:

```text
src/DesafioArquiteturaSoftware.Query/appsettings.json
```

A connection string utilizada pela Query API deve estar configurada na chave:

```text
ConnectionStrings:ReadDatabase
```

Exemplo de estrutura:

```json
{
  "ConnectionStrings": {
    "ReadDatabase": "Host=localhost;Port=<porta>;Database=<database>;Username=<usuario>;Password=<senha>"
  }
}
```

Utilize os valores correspondentes à Read Replica configurada no `docker-compose.yml`.

## 5. Executar a Query API

Abra outro terminal na raiz do projeto:

```powershell
dotnet run --project .\src\DesafioArquiteturaSoftware.Query
```

A URL da API será apresentada no terminal.

### Endpoint de saldo

```text
GET /api/accounts/{accountId}/balance
```

Exemplo:

```http
GET /api/accounts/00000000-0000-0000-0000-000000000001/balance
```

Exemplo com data:

```http
GET /api/accounts/00000000-0000-0000-0000-000000000001/balance?at=2026-09-29T19:00:00Z
```

Exemplo de resposta:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "balance": 3750.00,
  "asOf": "2026-09-29T19:49:14.47038Z"
}
```

## 6. Executar o fluxo completo

Para testar o sistema do início ao fim:

### Terminal 1 — Infraestrutura

Na raiz:

```powershell
docker compose up -d
```

Confirme:

```powershell
docker compose ps
```

### Terminal 2 — Write API

```powershell
dotnet run --project .\src\DesafioArquiteturaSoftware.Api
```

Mantenha o terminal aberto.

### Terminal 3 — Query API

```powershell
dotnet run --project .\src\DesafioArquiteturaSoftware.Query
```

Mantenha o terminal aberto.

### Teste de escrita

Crie uma movimentação através da Write API:

```http
POST /api/transactions
```

Com:

```text
Idempotency-Key: <chave-unica>
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

Crie também um débito:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "description": "Pagamento",
  "amount": 250.00,
  "type": 2
}
```

### Teste de leitura

Depois das movimentações estarem disponíveis na Read Replica:

```http
GET /api/accounts/00000000-0000-0000-0000-000000000001/balance
```

O resultado será calculado a partir das movimentações disponíveis no banco de leitura.

## 7. Postman

O projeto possui uma collection para facilitar os testes manuais:

```text
DesafioArquiteturaSoftware.postman_collection.json
```

### Importar a collection

No Postman:

1. Abra o Postman.
2. Clique em `Import`.
3. Selecione o arquivo:

```text
DesafioArquiteturaSoftware.postman_collection.json
```

4. Confirme a importação.

### Variáveis da collection

A collection possui as seguintes variáveis:

```text
writeUrl
queryUrl
accountId
transactionId
```

Ajuste `writeUrl` e `queryUrl` para as URLs apresentadas pelos respectivos comandos `dotnet run`.

Por exemplo:

```text
writeUrl = https://localhost:<porta-da-write-api>
queryUrl = https://localhost:<porta-da-query-api>
```

O `accountId` pode ser definido como:

```text
00000000-0000-0000-0000-000000000001
```

O `transactionId` é preenchido a partir da movimentação criada.

### Requests disponíveis

#### Write API

```text
Create Transaction - Credit
Create Transaction - Debit
Update Transaction
Delete Transaction
```

#### Query API

```text
Get Current Account Balance
Get Account Balance At
```

A collection também possui testes básicos de status HTTP.

## 8. Executar os testes automatizados

Na raiz do projeto:

```powershell
dotnet test
```

Para executar o build:

```powershell
dotnet build
```

Para executar ambos:

```powershell
dotnet build
dotnet test
```

Os projetos de teste são:

```text
src/DesafioArquiteturaSoftware.Tests
src/DesafioArquiteturaSoftware.Query.Tests
```

## 9. Verificar o estado da infraestrutura

Ver containers:

```powershell
docker compose ps
```

Ver todos os logs:

```powershell
docker compose logs
```

Acompanhar logs:

```powershell
docker compose logs -f
```

Reiniciar a infraestrutura:

```powershell
docker compose down
docker compose up -d
```

## 10. Problemas comuns

### `ReadDatabase connection string was not configured`

Verifique:

```text
src/DesafioArquiteturaSoftware.Query/appsettings.json
```

A configuração precisa conter:

```json
{
  "ConnectionStrings": {
    "ReadDatabase": "..."
  }
}
```

Também confirme que a aplicação que está sendo executada é:

```text
DesafioArquiteturaSoftware.Query
```

### `column ... does not exist`

Esse erro normalmente indica que o SQL da Query API não corresponde aos nomes das colunas existentes no PostgreSQL.

Verifique o schema do banco antes de alterar a consulta.

### Query API não encontra dados recém-criados

A Query API utiliza a Read Replica.

Pode existir um pequeno intervalo entre a gravação no Primary e a disponibilidade da alteração na réplica.

Verifique também o estado dos containers:

```powershell
docker compose ps
```

e os logs:

```powershell
docker compose logs -f
```

### Porta da API diferente da esperada

A porta efetivamente utilizada é apresentada pelo `dotnet run`.

Exemplo:

```text
Now listening on: https://localhost:7256
```

Use essa URL no Postman.

Também é possível consultar:

```text
src/DesafioArquiteturaSoftware.Api/Properties/launchSettings.json
src/DesafioArquiteturaSoftware.Query/Properties/launchSettings.json
```

## 11. Estrutura relacionada à execução

```text
.
├── docker-compose.yml
├── README.md
├── DesafioArquiteturaSoftware.sln
├── DesafioArquiteturaSoftware.postman_collection.json
├── docs/
│   └── adr/
└── src/
    ├── DesafioArquiteturaSoftware.Api/
    ├── DesafioArquiteturaSoftware.Application/
    ├── DesafioArquiteturaSoftware.Domain/
    ├── DesafioArquiteturaSoftware.Infrastructure/
    ├── DesafioArquiteturaSoftware.Tests/
    ├── DesafioArquiteturaSoftware.Query/
    └── DesafioArquiteturaSoftware.Query.Tests/
```

## 12. Execução rápida

Para quem já possui o ambiente configurado:

```powershell
# 1. Subir PostgreSQL
docker compose up -d

# 2. Terminal 1 - Write API
dotnet run --project .\src\DesafioArquiteturaSoftware.Api

# 3. Terminal 2 - Query API
dotnet run --project .\src\DesafioArquiteturaSoftware.Query

# 4. Terminal 3 - Testes
dotnet test
```

Depois, importe no Postman:

```text
DesafioArquiteturaSoftware.postman_collection.json
```

Configure:

```text
writeUrl
queryUrl
accountId
```

e execute os requests da collection.

## Documentação arquitetural

As decisões arquiteturais não são detalhadas neste README.

Elas estão documentadas individualmente em:

```text
docs/adr/
```

Cada ADR registra o contexto, a decisão, as alternativas consideradas e as consequências da decisão.
