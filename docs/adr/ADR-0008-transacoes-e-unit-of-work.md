# ADR-0008 — Transações e Unit of Work

- Status: Accepted
- Data: 2026-09-29

## Contexto

As operações de escrita da aplicação podem envolver mais de uma alteração persistida que precisa ser tratada como uma única operação.

O principal caso ocorre durante a criação de uma movimentação financeira, em que a aplicação precisa persistir:

- a movimentação financeira;
- o registro de idempotência.

Essas alterações precisam ser confirmadas juntas. Se uma delas falhar, a outra não deve permanecer persistida.

Além disso, as operações de atualização e exclusão também precisam garantir que uma falha durante a persistência não deixe o contexto em um estado inconsistente.

Foi necessário definir uma abstração responsável pelo ciclo de vida das transações sem expor detalhes específicos do Entity Framework Core para a camada de Application.

## Decisão

Utilizar o padrão Unit of Work através da abstração:

```text
IUnitOfWork
```

A interface é definida no Domain:

```text
Domain/
└── Interfaces/
    └── IUnitOfWork.cs
```

Sua responsabilidade é controlar as operações relacionadas à unidade de trabalho:

```text
BeginTransactionAsync()
SaveChangesAsync()
CommitAsync()
RollbackAsync()
ClearTracking()
```

A implementação utiliza Entity Framework Core e fica na Infrastructure:

```text
Infrastructure/
└── EfUnitOfWork.cs
```

Dessa forma, a Application depende somente da abstração:

```text
Application
     |
     v
IUnitOfWork
     ^
     |
Infrastructure
     |
     v
Entity Framework Core
```

## Fluxo transacional

No processo de criação de uma movimentação, a operação segue conceitualmente:

```text
BEGIN TRANSACTION
       |
       +-- Insert FinancialTransaction
       |
       +-- Insert IdempotencyRecord
       |
       +-- SaveChanges
       |
       v
     COMMIT
```

Caso alguma etapa falhe:

```text
BEGIN TRANSACTION
       |
       +-- operação
       |
       +-- erro
       |
       v
    ROLLBACK
```

Após uma falha, o contexto também tem seu tracking limpo para evitar que entidades de uma operação malsucedida permaneçam rastreadas pelo contexto.

## Motivações

### Atomicidade

A movimentação financeira e o registro de idempotência precisam possuir o mesmo resultado transacional.

Não seria aceitável concluir a criação da movimentação e falhar na persistência do registro de idempotência deixando o sistema em um estado parcial.

A transação garante:

```text
Tudo confirmado
ou
Tudo desfeito
```

### Abstração da infraestrutura

A Application não precisa conhecer:

```text
DbContext
DbTransaction
DbUpdateException
DbUpdateConcurrencyException
```

Esses detalhes permanecem na Infrastructure.

A Application trabalha com:

```text
IUnitOfWork
```

Isso mantém a dependência da camada de aplicação em abstrações definidas pela solução.

### Tratamento centralizado da persistência

A implementação da Unit of Work também centraliza o tratamento de exceções específicas do Entity Framework Core.

Por exemplo:

```text
DbUpdateConcurrencyException
        |
        v
ConcurrencyConflictException
```

Da mesma forma, uma violação da constraint de unicidade da chave de idempotência pode ser convertida em uma exceção específica da aplicação.

Isso impede que exceções específicas do PostgreSQL ou EF Core vazem diretamente para a camada de Application.

## Limpeza do tracking

Após uma falha transacional, a implementação executa:

```text
Rollback
   |
   v
ClearTracking
```

A limpeza evita que entidades alteradas durante uma operação que falhou continuem sendo acompanhadas pelo `DbContext`.

Isso é especialmente importante quando o contexto permanece disponível dentro do mesmo escopo de requisição.

## Alternativas consideradas

### Utilizar transações diretamente na Application

Uma alternativa seria permitir que os handlers utilizassem diretamente as APIs de transação do Entity Framework Core.

Essa abordagem faria a camada de Application depender do EF Core.

Isso aumentaria o acoplamento da aplicação com a tecnologia de persistência e faria detalhes de infraestrutura vazarem para os handlers.

Por esse motivo, essa alternativa não foi adotada.

### Confiar apenas no SaveChanges

Outra possibilidade seria utilizar somente:

```text
DbContext.SaveChangesAsync()
```

sem controlar explicitamente a transação.

Para alterações simples, o Entity Framework Core já oferece comportamento transacional adequado para o próprio `SaveChanges`.

Entretanto, a solução possui operações em que o limite transacional é uma decisão explícita do caso de uso, principalmente na criação conjunta da movimentação e do registro de idempotência.

Por esse motivo, foi adotada uma Unit of Work com controle explícito da transação.

### Utilizar uma biblioteca externa de Unit of Work

Também seria possível introduzir uma biblioteca ou framework específico para implementar o padrão.

Isso adicionaria uma dependência externa para uma responsabilidade que pode ser atendida diretamente pelo Entity Framework Core.

Para o escopo atual, a implementação própria foi considerada mais simples e transparente.

## Consequências

### Positivas

- Garante atomicidade das operações que fazem parte do mesmo caso de uso.
- Mantém detalhes do EF Core na Infrastructure.
- Permite controlar explicitamente `commit` e `rollback`.
- Centraliza o tratamento de determinadas exceções de persistência.
- Permite limpar o tracking após falhas.
- Facilita testes da Application através da abstração `IUnitOfWork`.

### Negativas

- Introduz uma abstração adicional.
- O ciclo de vida da transação precisa ser corretamente controlado pelos handlers.
- O código dos casos de uso possui etapas explícitas de `Begin`, `Commit` e `Rollback`.
- A implementação continua dependente das características transacionais do banco utilizado.

## Responsabilidades

A divisão de responsabilidades ficou:

```text
Application
    |
    +-- Define quando uma operação precisa de uma unidade transacional.
    |
    v
IUnitOfWork
    |
    v
Infrastructure
    |
    +-- Implementa a transação.
    +-- Executa SaveChanges.
    +-- Traduz exceções do EF Core.
    +-- Realiza Rollback.
    +-- Limpa o tracking.
```

A Unit of Work não contém regras de negócio.

Sua responsabilidade é controlar a persistência transacional da operação.

## Relação com outras decisões

Esta decisão está diretamente relacionada a:

- [ADR-0003 — Idempotência nas Operações de Criação](ADR-0003-idempotencia-nas-operacoes-de-criacao.md)
- [ADR-0004 — Controle de Concorrência Otimista](ADR-0004-controle-de-concorrencia-otimista.md)
- [ADR-0009 — Separação de Domain, Application e Infrastructure](ADR-0009-separacao-de-camadas.md)

A idempotência depende da atomicidade entre a movimentação e seu registro de idempotência.

O controle de concorrência depende da tradução das exceções de persistência realizada na Infrastructure.

## Status

Accepted.
