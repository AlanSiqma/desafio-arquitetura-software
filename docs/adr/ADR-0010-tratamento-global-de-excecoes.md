# ADR-0010 — Tratamento Global de Exceções

- Status: Accepted
- Data: 2026-09-29

## Contexto

A Write API possui diferentes tipos de falhas que precisam ser convertidas em respostas HTTP apropriadas.

Alguns exemplos são:

- dados de entrada inválidos;
- recurso não encontrado;
- conflito de idempotência;
- conflito de concorrência;
- erros inesperados.

Sem uma estratégia centralizada, cada endpoint poderia precisar implementar seu próprio tratamento de exceções, resultando em duplicação e respostas inconsistentes.

Além disso, detalhes de infraestrutura não devem ser expostos diretamente ao consumidor da API.

## Decisão

Utilizar o mecanismo global de tratamento de exceções do ASP.NET Core através de um handler centralizado:

```text
GlobalExceptionHandler
```

O handler é registrado na inicialização da aplicação:

```text
AddExceptionHandler<GlobalExceptionHandler>()
```

e habilitado através de:

```text
UseExceptionHandler()
```

A aplicação também registra:

```text
AddProblemDetails()
```

O fluxo passa a ser:

```text
Endpoint
   |
   v
Application
   |
   +---- exceção
          |
          v
GlobalExceptionHandler
          |
          v
HTTP Response
```

## Mapeamento de exceções

As exceções conhecidas são convertidas para códigos HTTP específicos.

### ArgumentException

Indica dados inválidos enviados para a operação.

Resposta:

```text
HTTP 400 Bad Request
```

### KeyNotFoundException

Indica que o recurso solicitado não foi encontrado.

Resposta:

```text
HTTP 404 Not Found
```

### IdempotencyRequestMismatchException

Indica que uma chave de idempotência existente foi reutilizada com dados diferentes.

Resposta:

```text
HTTP 409 Conflict
```

### ConcurrencyConflictException

Indica que uma alteração concorrente foi detectada.

Resposta:

```text
HTTP 409 Conflict
```

### Exceções não tratadas

Qualquer exceção que não possua um mapeamento específico é tratada como erro interno.

Resposta:

```text
HTTP 500 Internal Server Error
```

## Motivações

### Consistência das respostas

O tratamento centralizado evita que cada endpoint defina individualmente como uma exceção deve ser convertida para HTTP.

Isso mantém um comportamento uniforme na API.

### Separação entre domínio e transporte

As exceções utilizadas pela Application não precisam conhecer códigos HTTP.

Por exemplo:

```text
ConcurrencyConflictException
```

representa um conflito da aplicação.

O conhecimento de que esse conflito deve resultar em:

```text
409 Conflict
```

fica na camada HTTP.

### Não expor detalhes de infraestrutura

Exceções específicas do Entity Framework Core ou PostgreSQL são tratadas na Infrastructure quando necessário.

A API recebe exceções apropriadas para o contexto da aplicação e não precisa conhecer detalhes internos do banco.

Fluxo:

```text
PostgreSQL / EF Core
        |
        v
Infrastructure
        |
        v
Application Exception
        |
        v
GlobalExceptionHandler
        |
        v
HTTP Response
```

### Redução de duplicação

Os endpoints permanecem focados em:

- receber a requisição;
- criar o command/query;
- executar o handler;
- retornar o resultado.

Eles não precisam repetir blocos de `try/catch` para cada tipo conhecido de exceção.

## Alternativas consideradas

### Tratamento individual em cada endpoint

Uma alternativa seria implementar:

```text
try
{
    ...
}
catch (...)
{
    ...
}
```

em cada endpoint.

Essa abordagem permitiria controle local, mas criaria duplicação e aumentaria a possibilidade de respostas diferentes para o mesmo tipo de erro.

Por esse motivo, não foi adotada.

### Middleware customizado manual

Outra alternativa seria criar um middleware próprio para capturar todas as exceções.

Embora seja uma abordagem válida, o ASP.NET Core já fornece uma infraestrutura específica para tratamento global de exceções.

Foi utilizado o mecanismo nativo para reduzir código próprio.

## Consequências

### Positivas

- Respostas HTTP padronizadas.
- Menor duplicação nos endpoints.
- Separação entre exceções da aplicação e códigos HTTP.
- Menor exposição de detalhes de infraestrutura.
- Facilidade para adicionar novos mapeamentos de exceções.

### Negativas

- O comportamento HTTP passa a depender de uma configuração central.
- Novas exceções relevantes precisam ser adicionadas ao mapeamento.
- Um erro de configuração do handler pode afetar o tratamento de toda a API.

## Responsabilidades

A divisão de responsabilidades é:

```text
Domain
  |
  +-- Define regras e exceções do domínio
  |
  v
Application
  |
  +-- Executa casos de uso
  |
  v
Infrastructure
  |
  +-- Traduz exceções técnicas quando necessário
  |
  v
API
  |
  +-- Traduz exceções da aplicação para HTTP
```

## Relação com outras decisões

Esta decisão está diretamente relacionada a:

- [ADR-0004 — Controle de Concorrência Otimista](ADR-0004-controle-de-concorrencia-otimista.md)
- [ADR-0008 — Transações e Unit of Work](ADR-0008-transacoes-e-unit-of-work.md)
- [ADR-0009 — Separação entre Domain, Application e Infrastructure](ADR-0009-separacao-de-camadas.md)

O controle de concorrência e as operações transacionais podem gerar exceções que atravessam as camadas até chegar ao tratamento global da API.

## Status

Accepted.
