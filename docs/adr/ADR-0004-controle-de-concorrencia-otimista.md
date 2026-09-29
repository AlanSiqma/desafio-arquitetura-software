# ADR-0004 — Controle de Concorrência Otimista

- Status: Accepted
- Data: 2026-09-29

## Contexto

A Write API permite a atualização de movimentações financeiras através do endpoint:

```text
PUT /api/transactions/{id}
```

Em um sistema distribuído, duas requisições podem tentar alterar a mesma movimentação aproximadamente ao mesmo tempo.

Sem um mecanismo de controle de concorrência, uma atualização poderia sobrescrever silenciosamente outra atualização realizada por um processo concorrente.

Esse comportamento é conhecido como lost update e pode causar inconsistências no estado da movimentação.

A solução precisa detectar esse cenário e informar ao cliente que houve conflito.

## Decisão

Utilizar controle de concorrência otimista através de uma propriedade de versão na entidade `FinancialTransaction`.

A entidade possui uma propriedade interna:

```csharp
public Guid Version { get; private set; }
```

A propriedade é configurada no Entity Framework Core como token de concorrência:

```csharp
builder.Property(x => x.Version)
    .IsConcurrencyToken()
    .IsRequired();
```

A versão é alterada sempre que a movimentação sofre uma alteração relevante.

Na criação:

```text
Version = Guid.NewGuid()
```

Na atualização:

```text
Version = Guid.NewGuid()
```

Na exclusão lógica:

```text
Version = Guid.NewGuid()
```

A versão não faz parte do contrato público da API.

## Funcionamento

O Entity Framework Core utiliza o valor da versão como parte do controle de concorrência durante a persistência.

Quando uma entidade é carregada, sua versão atual é conhecida pelo contexto.

Durante a atualização, o mecanismo de persistência verifica se o registro ainda corresponde à versão esperada.

Conceitualmente:

```text
Banco:

Transaction
Version = A
```

Processo A lê:

```text
Version = A
```

Processo B lê:

```text
Version = A
```

Processo A atualiza:

```text
Version A -> B
```

Processo B tenta atualizar utilizando a versão que havia lido:

```text
Version esperada = A
Version atual    = B
```

O conflito é detectado pelo mecanismo de concorrência.

## Tratamento do conflito

A infraestrutura captura a exceção específica do Entity Framework Core:

```text
DbUpdateConcurrencyException
```

A exceção é convertida em uma exceção de domínio:

```text
ConcurrencyConflictException
```

Dessa forma, a camada de Application não precisa possuir dependência direta do Entity Framework Core.

O fluxo é:

```text
EF Core
   |
   | DbUpdateConcurrencyException
   v
Infrastructure
   |
   | ConcurrencyConflictException
   v
Application
   |
   v
API
   |
   v
HTTP 409 Conflict
```

## Motivações

### Evitar sobrescrita silenciosa

O principal objetivo é evitar que uma atualização concorrente simplesmente substitua uma alteração realizada por outro processo.

Quando existe conflito, a operação é rejeitada explicitamente.

### Não bloquear recursos durante a leitura

A estratégia otimista não mantém locks de banco durante todo o período entre leitura e atualização.

Isso reduz a necessidade de bloqueios prolongados e é adequado para operações em que conflitos são esperados como exceção, e não como regra.

### Manter a versão interna

A versão não é exposta ao cliente.

O cliente utiliza apenas o identificador da movimentação:

```text
PUT /api/transactions/{id}
```

O controle de concorrência permanece uma responsabilidade interna da persistência.

## Alternativas consideradas

### Concorrência pessimista

Uma alternativa seria utilizar locks explícitos durante a leitura e atualização do registro.

Essa estratégia poderia impedir determinadas alterações concorrentes enquanto o recurso estivesse bloqueado.

Entretanto, aumentaria a utilização de locks e poderia reduzir a capacidade de processamento em cenários com maior concorrência.

Para o fluxo atual, foi adotada a estratégia otimista.

### Não utilizar controle de concorrência

Outra possibilidade seria permitir que a última atualização simplesmente sobrescrevesse a anterior.

Essa abordagem é mais simples, porém não permite detectar alterações concorrentes e pode resultar em perda silenciosa de atualizações.

Por esse motivo, não foi adotada.

### Expor a versão na API

Também seria possível utilizar um modelo em que o cliente recebesse a versão e a enviasse explicitamente na atualização.

Essa estratégia permitiria um controle explícito pelo consumidor da API.

Entretanto, para o escopo atual, a versão foi mantida como detalhe interno da persistência, mantendo o contrato:

```text
PUT /api/transactions/{id}
```

sem necessidade de um campo `Version` no request.

## Consequências

### Positivas

- Detecta alterações concorrentes.
- Evita sobrescrita silenciosa de alterações.
- Não exige locks prolongados durante o fluxo normal.
- Mantém a versão fora do contrato público.
- Permite converter detalhes específicos do EF Core em exceções de domínio.
- Retorna conflito explícito ao cliente através do HTTP 409.

### Negativas

- Uma operação pode falhar quando outra alteração concorrente ocorrer primeiro.
- O cliente precisa tratar respostas `409 Conflict`.
- O mecanismo depende do controle de versão persistido no registro.
- A infraestrutura possui lógica adicional para traduzir a exceção de concorrência.

## Relação com outras decisões

Esta decisão complementa o mecanismo de idempotência definido em:

- [ADR-0003 — Idempotência nas Operações de Criação](ADR-0003-idempotencia-nas-operacoes-de-criacao.md)

Os dois mecanismos tratam problemas diferentes:

```text
Idempotência
    |
    +-- Evita duplicação de uma mesma operação

Concorrência
    |
    +-- Detecta alterações concorrentes no mesmo recurso
```

Também está relacionada à separação de responsabilidades definida na arquitetura da Write API.

O Entity Framework Core permanece na Infrastructure, enquanto Application trabalha apenas com abstrações do domínio.

## Status

Accepted.
