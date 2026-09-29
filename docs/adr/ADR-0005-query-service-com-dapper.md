# ADR-0005 — Query Service com Dapper

- Status: Accepted
- Data: 2026-09-29

## Contexto

A arquitetura possui um microserviço específico para consultas:

```text
DesafioArquiteturaSoftware.Query
```

Esse serviço é responsável por consultar o saldo consolidado das contas e não realiza operações de escrita.

A consulta principal precisa realizar uma agregação sobre as movimentações financeiras:

```text
Créditos → soma positiva
Débitos  → soma negativa
```

Como a Query API possui responsabilidade exclusivamente de leitura, foi necessário escolher uma estratégia de acesso aos dados adequada a esse cenário.

A Write API utiliza Entity Framework Core para o fluxo de persistência e regras relacionadas à escrita. Para a Query API, foi considerada uma abordagem mais direta para executar SQL de leitura.

## Decisão

Utilizar Dapper como ferramenta de acesso a dados na Query API.

O fluxo de consulta será:

```text
HTTP Request
     |
     v
Endpoint
     |
     v
GetAccountBalanceHandler
     |
     v
IAccountBalanceRepository
     |
     v
AccountBalanceRepository
     |
     v
Dapper
     |
     v
Npgsql
     |
     v
PostgreSQL Read Replica
```

A Query API utiliza SQL explícito para as consultas.

A consulta de saldo realiza a agregação diretamente no PostgreSQL, evitando carregar todas as movimentações para a aplicação.

Conceitualmente:

```sql
SUM(
    CASE
        WHEN Type = Credit THEN Amount
        WHEN Type = Debit THEN -Amount
    END
)
```

O resultado é projetado diretamente para o modelo de consulta:

```text
AccountBalance
-------------------------
AccountId
Balance
AsOf
```

## Motivações

### Consultas SQL explícitas

Dapper permite que a Query API utilize SQL diretamente.

Isso é adequado para consultas que possuem projeções e agregações específicas, como o cálculo do saldo consolidado.

A consulta permanece explícita no código e pode ser otimizada diretamente de acordo com o banco utilizado.

### Menor complexidade para leitura

A Query API não precisa dos recursos de rastreamento de entidades utilizados no fluxo de escrita.

Como seu objetivo é consultar dados e projetar resultados, um micro-ORM como Dapper reduz a quantidade de abstrações necessárias.

### Separação entre escrita e leitura

A escolha do Dapper não interfere na utilização do Entity Framework Core pela Write API.

Cada microserviço pode utilizar a ferramenta mais adequada ao seu respectivo fluxo sem criar uma dependência entre eles.

```text
Write API
    |
    +-- Entity Framework Core
    |
    +-- PostgreSQL Primary


Query API
    |
    +-- Dapper
    |
    +-- PostgreSQL Read Replica
```

### Projeções específicas

A Query API não precisa reconstruir entidades completas para responder à consulta.

O resultado necessário é uma projeção específica:

```text
AccountBalance
```

Isso mantém o modelo de consulta separado das entidades utilizadas pela Write API.

## Estrutura utilizada

A Query API mantém o acesso a dados isolado através de uma abstração:

```text
Repositories/
├── IAccountBalanceRepository.cs
└── AccountBalanceRepository.cs
```

A implementação utiliza uma fábrica de conexão:

```text
Data/
└── QueryDbConnectionFactory.cs
```

A connection string utilizada é:

```text
ConnectionStrings:ReadDatabase
```

Dessa forma, a Query API não precisa conhecer detalhes de configuração de conexão em seus handlers.

## Alternativas consideradas

### Entity Framework Core na Query API

Uma alternativa seria utilizar Entity Framework Core também para as consultas.

Essa abordagem manteria uma única tecnologia de acesso a dados em toda a solução.

Entretanto, a Query API possui uma necessidade predominantemente orientada a consultas e projeções SQL. Para esse cenário, foi considerado desnecessário introduzir o tracking e outras abstrações do ORM completo.

Por esse motivo, Dapper foi escolhido para o fluxo de leitura.

### Acessar o banco através da Write API

Outra alternativa seria fazer a Query API consumir um endpoint da Write API para obter o saldo.

Essa abordagem adicionaria uma dependência síncrona entre os dois serviços:

```text
Query API
    |
    v
Write API
    |
    v
Database
```

Isso reduziria o isolamento entre os serviços e faria a disponibilidade da Query API depender diretamente da Write API.

Como a arquitetura possui uma Read Replica, a Query API pode consultar os dados diretamente na infraestrutura destinada à leitura.

### Implementar uma camada de cache

Também seria possível utilizar cache para armazenar saldos previamente calculados.

Essa abordagem poderia reduzir consultas repetidas ao banco, mas introduziria outra camada de consistência e invalidação.

Para o escopo atual, o acesso direto à Read Replica foi considerado suficiente.

## Consequências

### Positivas

- SQL de leitura explícito.
- Adequado para consultas com agregações e projeções.
- Baixo overhead para operações de leitura.
- Separação clara entre modelos de consulta e entidades de escrita.
- Não cria dependência da Query API com a Write API.
- Permite utilizar diretamente a Read Replica.
- A consulta pode ser otimizada de acordo com o banco PostgreSQL.

### Negativas

- A Query API passa a possuir SQL específico do PostgreSQL.
- A aplicação precisa lidar diretamente com detalhes do banco em determinadas consultas.
- A solução utiliza duas tecnologias de acesso a dados diferentes.
- Alterações no schema do banco podem exigir ajustes explícitos nas consultas SQL.

## Relação com outras decisões

Esta decisão depende diretamente de:

- [ADR-0001 — Separação entre Write API e Query API](ADR-0001-separacao-write-e-query.md)
- [ADR-0002 — PostgreSQL com Read Replica](ADR-0002-postgresql-com-read-replica.md)

A separação dos serviços permite que a Query API tenha uma estratégia de acesso a dados independente da Write API.

A utilização da Read Replica fornece a fonte de dados destinada às consultas executadas pelo Dapper.

## Status

Accepted.
