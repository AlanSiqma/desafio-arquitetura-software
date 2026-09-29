# ADR-0009 — Separação entre Domain, Application e Infrastructure

- Status: Accepted
- Data: 2026-09-29

## Contexto

A Write API possui responsabilidades diferentes relacionadas ao processamento das movimentações financeiras.

Existem regras de negócio que não devem depender de tecnologia de persistência, enquanto detalhes como Entity Framework Core, PostgreSQL e tratamento de exceções de banco pertencem à infraestrutura.

Para manter essas responsabilidades separadas, a solução foi organizada em camadas:

```text
Api
Application
Domain
Infrastructure
```

A arquitetura precisa permitir que as regras de negócio sejam testadas sem depender diretamente do banco de dados ou do framework de persistência.

## Decisão

Separar a Write API em camadas com responsabilidades específicas e dependências direcionadas para dentro da aplicação.

A estrutura adotada é:

```text
src/
├── DesafioArquiteturaSoftware.Api/
├── DesafioArquiteturaSoftware.Application/
├── DesafioArquiteturaSoftware.Domain/
└── DesafioArquiteturaSoftware.Infrastructure/
```

A direção das dependências é:

```text
Api
 |
 +------> Application
 |
 +------> Infrastructure
            |
            +------> Application
            |
            +------> Domain

Application
 |
 +------> Domain
```

O Domain não depende das demais camadas.

## Domain

O Domain concentra as regras e conceitos centrais do negócio.

Exemplos:

```text
Entities/
├── FinancialTransaction.cs
└── IdempotencyRecord.cs

Enums/
└── TransactionType.cs

Exceptions/
├── IdempotencyKeyAlreadyExistsException.cs
├── IdempotencyRequestMismatchException.cs
└── ConcurrencyConflictException.cs

Interfaces/
├── IFinancialTransactionRepository.cs
├── IIdempotencyRepository.cs
└── IUnitOfWork.cs
```

A entidade `FinancialTransaction` contém regras relacionadas ao próprio estado da movimentação, como:

- validação dos dados;
- atualização;
- exclusão lógica;
- controle da versão.

O Domain não conhece:

- Entity Framework Core;
- PostgreSQL;
- HTTP;
- Dapper;
- ASP.NET Core.

## Application

A Application representa os casos de uso da aplicação.

Os comandos são organizados por operação:

```text
Commands/
├── CreateFinancialTransaction/
├── UpdateFinancialTransaction/
└── DeleteFinancialTransaction/
```

Os handlers coordenam o fluxo do caso de uso utilizando as abstrações do Domain.

Exemplo conceitual:

```text
CreateFinancialTransactionHandler
        |
        +-- IIdempotencyRepository
        |
        +-- IFinancialTransactionRepository
        |
        +-- IUnitOfWork
        |
        v
      Domain
```

A Application não referencia diretamente o Entity Framework Core.

## Infrastructure

A Infrastructure implementa os detalhes técnicos necessários para persistência e integração com o banco.

Exemplos:

```text
Infrastructure/
├── Data/
├── Repositories/
├── Configurations/
└── EfUnitOfWork.cs
```

Essa camada é responsável por:

- `DbContext`;
- configurações do Entity Framework Core;
- migrations;
- implementação dos repositories;
- implementação da Unit of Work;
- tratamento de exceções específicas do EF Core e PostgreSQL.

Exemplo:

```text
DbUpdateConcurrencyException
        |
        v
EfUnitOfWork
        |
        v
ConcurrencyConflictException
```

Dessa forma, a Application recebe uma exceção pertencente ao modelo da aplicação, sem depender da exceção específica do Entity Framework Core.

## API

A camada de API é responsável pela exposição HTTP.

Ela contém:

```text
Contracts/
Endpoints/
Exceptions/
Program.cs
```

Suas responsabilidades incluem:

- receber requisições HTTP;
- transformar requests em commands;
- executar handlers;
- transformar resultados em respostas HTTP;
- tratar exceções através do mecanismo global de exceções.

A API não contém as regras centrais de negócio.

Fluxo:

```text
HTTP Request
     |
     v
Endpoint
     |
     v
Command
     |
     v
Application Handler
     |
     v
Domain
     |
     v
Infrastructure
     |
     v
PostgreSQL
```

## Motivações

### Separação de responsabilidades

Cada camada possui uma responsabilidade definida.

Isso evita que regras de negócio, transporte HTTP e persistência sejam implementados no mesmo local.

### Independência do domínio

O Domain não depende de Entity Framework Core ou ASP.NET Core.

Isso permite testar as regras da entidade sem necessidade de infraestrutura externa.

### Testabilidade

A Application utiliza interfaces para acessar persistência.

Por exemplo:

```text
IFinancialTransactionRepository
IIdempotencyRepository
IUnitOfWork
```

Nos testes, essas abstrações podem ser substituídas por mocks ou implementações de teste.

### Controle do acoplamento

As dependências de tecnologia ficam concentradas na Infrastructure e na API.

A regra principal é evitar que detalhes externos sejam propagados para as camadas internas.

## Alternativas consideradas

### Arquitetura sem separação de camadas

Uma alternativa seria implementar endpoints, regras de negócio e acesso ao banco diretamente na API.

Exemplo:

```text
Endpoint
   |
   +-- regra de negócio
   |
   +-- DbContext
   |
   +-- SQL
```

Essa abordagem reduziria a quantidade inicial de projetos e arquivos, mas aumentaria o acoplamento entre transporte, domínio e persistência.

Também dificultaria testes isolados das regras de negócio.

Por esse motivo, não foi adotada.

### Clean Architecture com maior quantidade de projetos

Outra possibilidade seria separar ainda mais as responsabilidades em projetos específicos.

Embora isso possa ser útil em sistemas maiores, adicionaria complexidade estrutural ao desafio sem uma necessidade proporcional.

Foi adotada uma separação em quatro projetos principais para a Write API, mantendo a arquitetura clara sem criar granularidade excessiva.

## Consequências

### Positivas

- Regras de negócio isoladas de frameworks.
- Application independente de detalhes do EF Core.
- Infrastructure concentrando detalhes de persistência.
- API concentrando responsabilidades HTTP.
- Maior facilidade para testes unitários.
- Menor acoplamento entre domínio e tecnologia.
- Possibilidade de substituir detalhes de infraestrutura sem alterar as regras centrais.

### Negativas

- Maior quantidade de projetos.
- Necessidade de definir corretamente as dependências entre camadas.
- Algumas operações simples exigem passagem por mais de uma camada.
- A estrutura inicial possui mais complexidade que uma aplicação monolítica sem separação.

## Regra de dependência

A principal regra adotada é:

```text
Domain
  ^
  |
Application
  ^
  |
Api / Infrastructure
```

As camadas externas podem depender das abstrações internas, enquanto o Domain não depende de infraestrutura ou transporte.

A Infrastructure implementa as interfaces definidas pelas camadas internas.

## Relação com outras decisões

Esta decisão está diretamente relacionada a:

- [ADR-0004 — Controle de Concorrência Otimista](ADR-0004-controle-de-concorrencia-otimista.md)
- [ADR-0008 — Transações e Unit of Work](ADR-0008-transacoes-e-unit-of-work.md)

O controle de concorrência é implementado na Infrastructure e exposto à Application através de exceções da aplicação.

A Unit of Work é definida através de uma abstração utilizada pela Application e implementada pela Infrastructure.

## Status

Accepted.
