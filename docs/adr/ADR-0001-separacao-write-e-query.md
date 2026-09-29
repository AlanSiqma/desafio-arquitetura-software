# ADR-0001 — Separação entre Write API e Query API

- Status: Accepted
- Data: 2026-09-29

## Contexto

O sistema precisa registrar movimentações financeiras e disponibilizar consultas do saldo consolidado de uma conta.

As operações de escrita e leitura possuem características diferentes.

As operações de escrita precisam garantir:

- consistência dos dados;
- controle de idempotência;
- controle de concorrência;
- transações;
- persistência confiável das movimentações.

As operações de consulta, por outro lado, possuem uma característica predominantemente orientada à leitura e podem apresentar uma carga elevada sem necessariamente exigir as mesmas características das operações de escrita.

O desafio também exige que a solução considere cenários de alta demanda, instabilidade e falhas parciais.

Diante dessas características, foi adotada uma arquitetura com dois microserviços:

- Write API;
- Query API.

## Decisão

Separar as responsabilidades de escrita e consulta em dois microserviços independentes.

### Write API

A `DesafioArquiteturaSoftware.Api` é responsável pelas operações de escrita:

```text
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
```

Essa API concentra as regras relacionadas à alteração do estado das movimentações financeiras.

O fluxo principal é:

```text
Client
  |
  v
Write API
  |
  v
Application
  |
  v
Domain
  |
  v
Infrastructure
  |
  v
PostgreSQL Primary
```

### Query API

A `DesafioArquiteturaSoftware.Query` é responsável pelas consultas.

Seu principal endpoint é:

```text
GET /api/accounts/{accountId}/balance
```

O fluxo de consulta é:

```text
Client
  |
  v
Query API
  |
  v
Query Repository
  |
  v
Dapper
  |
  v
PostgreSQL Read Replica
```

A Query API não possui dependência dos projetos da Write API.

## Motivações

### Separação de responsabilidades

A escrita e a consulta possuem necessidades diferentes.

A Write API concentra regras de negócio e mecanismos de consistência, enquanto a Query API é especializada em leitura.

Isso evita que a API responsável pela escrita acumule responsabilidades de consulta.

### Escalabilidade independente

A separação permite escalar os serviços de acordo com suas respectivas necessidades.

Por exemplo, caso a quantidade de consultas seja significativamente maior que a quantidade de escritas, as instâncias da Query API podem ser escaladas independentemente da Write API.

```text
                  +----------------+
                  |    Clientes    |
                  +-------+--------+
                          |
             +------------+------------+
             |                         |
             v                         v
       +-----------+             +-----------+
       | Write API |             | Query API |
       +-----------+             +-----------+
             |                    /    |                 v                   /     |            PostgreSQL           Query API instances
         Primary                   |
             |                     v
             +-------------> Read Replica
```

### Isolamento de carga

Consultas de saldo não precisam disputar recursos diretamente com as operações de escrita da API.

A separação permite que a infraestrutura de leitura seja otimizada para consultas sem alterar o fluxo responsável pela persistência.

### Evolução independente

As necessidades da camada de consulta podem evoluir independentemente das necessidades da camada de escrita.

Por exemplo, a Query API pode adotar consultas SQL específicas, projeções e otimizações de leitura sem introduzir essas preocupações na aplicação responsável pelas operações de escrita.

## Alternativas consideradas

### Uma única API para leitura e escrita

Uma alternativa seria utilizar uma única API contendo endpoints de escrita e leitura.

Exemplo:

```text
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
GET    /api/accounts/{accountId}/balance
```

Essa abordagem seria mais simples inicialmente e reduziria a quantidade de projetos e processos.

Porém, ela acoplaria as características de leitura e escrita em uma mesma aplicação, dificultando o isolamento de carga e a evolução independente dos dois fluxos.

Por esse motivo, essa alternativa não foi adotada.

### Separação em mais camadas/projetos

Outra possibilidade seria criar projetos independentes para cada camada da Query API, por exemplo:

```text
Query.Api
Query.Application
Query.Domain
Query.Infrastructure
```

Essa abordagem aumentaria a separação física das responsabilidades.

Entretanto, para o escopo do desafio, isso adicionaria complexidade estrutural sem benefício proporcional.

Foi adotado um único projeto para a Query API, utilizando separação lógica por diretórios:

```text
Query/
├── Contracts/
├── Queries/
├── Models/
├── Repositories/
├── Data/
└── Endpoints/
```

## Consequências

### Positivas

- Separação clara entre leitura e escrita.
- Possibilidade de escalar leitura e escrita independentemente.
- Possibilidade de direcionar consultas para uma réplica de leitura.
- Menor acoplamento entre os fluxos de leitura e escrita.
- Evolução independente das duas APIs.
- Permite utilizar estratégias de persistência diferentes para cada necessidade.

### Negativas

- A solução possui mais de um processo para executar localmente.
- A implantação possui maior complexidade que uma API monolítica.
- É necessário configurar comunicação e observabilidade dos dois serviços.
- A utilização de uma réplica de leitura introduz possibilidade de atraso de replicação.
- A consistência observada pela Query API pode ser diferente da consistência do banco primário durante o intervalo de replicação.

## Relação com outras decisões

Esta decisão fundamenta outras decisões arquiteturais do projeto:

- [ADR-0002 — PostgreSQL com Read Replica](ADR-0002-postgresql-com-read-replica.md)
- [ADR-0005 — Query Service com Dapper](ADR-0005-query-service-com-dapper.md)

A separação entre Write API e Query API é o que permite direcionar as consultas para uma infraestrutura de leitura independente.

## Status

Accepted.
