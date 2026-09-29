# ADR-0007 — ASP.NET Core Minimal API

- Status: Accepted
- Data: 2026-09-29

## Contexto

As APIs do projeto são construídas com ASP.NET Core e possuem um conjunto relativamente pequeno de endpoints.

A solução precisa disponibilizar endpoints HTTP para:

- registrar movimentações financeiras;
- atualizar movimentações;
- excluir movimentações;
- consultar o saldo consolidado de uma conta.

Além disso, a arquitetura separa as responsabilidades entre Write API e Query API.

Foi necessário escolher a abordagem para definição dos endpoints HTTP sem adicionar uma camada de abstração desnecessária.

## Decisão

Utilizar ASP.NET Core Minimal API para a exposição dos endpoints HTTP.

Os endpoints são definidos diretamente no processo de inicialização das aplicações e organizados em classes específicas de extensão.

Na Write API:

```text
Endpoints/
└── FinancialTransactionEndpoints.cs
```

Na Query API:

```text
Endpoints/
└── AccountEndpoints.cs
```

A configuração da aplicação permanece no `Program.cs`, enquanto o mapeamento dos endpoints fica separado por responsabilidade.

Exemplo conceitual:

```text
Program.cs
    |
    v
MapFinancialTransactionEndpoints()
```

e:

```text
Program.cs
    |
    v
MapAccountEndpoints()
```

## Motivações

### Baixo overhead estrutural

O projeto possui APIs com quantidade limitada de endpoints e não necessita de uma estrutura baseada em controllers para atender ao escopo atual.

Minimal API permite expor os endpoints diretamente, mantendo uma estrutura simples.

### Separação entre transporte e aplicação

Embora os endpoints sejam definidos utilizando Minimal API, as regras de negócio não ficam dentro dos delegates HTTP.

O fluxo da Write API segue:

```text
HTTP Endpoint
      |
      v
Command
      |
      v
Handler
      |
      v
Domain
```

Na Query API:

```text
HTTP Endpoint
      |
      v
Query
      |
      v
Handler
      |
      v
Repository
```

Dessa forma, a escolha por Minimal API não elimina a separação das responsabilidades internas.

### Organização por domínio da API

Os endpoints são agrupados por responsabilidade.

Write API:

```text
/api/transactions
```

Query API:

```text
/api/accounts/{accountId}/balance
```

Isso mantém os contratos HTTP próximos das funcionalidades que expõem.

## Alternativas consideradas

### Controllers do ASP.NET Core

Uma alternativa seria utilizar controllers tradicionais:

```text
Controllers/
├── TransactionsController.cs
└── AccountsController.cs
```

Controllers são uma abordagem consolidada no ASP.NET Core e poderiam atender aos mesmos requisitos.

Entretanto, para o tamanho atual das APIs, essa estrutura adicionaria elementos de infraestrutura HTTP que não são necessários para o fluxo atual.

Por esse motivo, Minimal API foi escolhida.

### Uma única classe contendo todos os endpoints

Outra possibilidade seria definir todos os endpoints diretamente no `Program.cs`.

Essa abordagem reduziria a quantidade de arquivos, mas faria o arquivo de inicialização acumular responsabilidades de configuração e definição de rotas.

Foi adotada uma separação por classes de extensão:

```text
Endpoints/
├── FinancialTransactionEndpoints.cs
└── AccountEndpoints.cs
```

## Consequências

### Positivas

- Estrutura HTTP simples.
- Menor quantidade de código de infraestrutura.
- Endpoints organizados por responsabilidade.
- Integração direta com Dependency Injection do ASP.NET Core.
- Mantém handlers e regras de negócio separados dos delegates HTTP.
- Adequado ao tamanho atual das APIs.

### Negativas

- A equipe precisa manter uma convenção clara para organização dos endpoints.
- O crescimento significativo da API pode exigir uma reorganização adicional.
- Controllers podem oferecer uma estrutura mais familiar para equipes acostumadas ao padrão MVC.

## Relação com outras decisões

A utilização de Minimal API complementa a separação de responsabilidades definida na arquitetura.

A decisão não altera as responsabilidades das camadas de Application, Domain ou Infrastructure.

Na Write API:

```text
Minimal API
    |
    v
Application
    |
    v
Domain
    |
    v
Infrastructure
```

Na Query API:

```text
Minimal API
    |
    v
Queries
    |
    v
Repositories
    |
    v
Read Replica
```

## Status

Accepted.
