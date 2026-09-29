# ADR-0003 — Idempotência nas Operações de Criação

- Status: Accepted
- Data: 2026-09-29

## Contexto

A Write API registra movimentações financeiras por meio do endpoint:

```text
POST /api/transactions
```

Em um ambiente distribuído, uma requisição pode ser processada pelo servidor e o cliente pode não receber a resposta devido a timeout, falha de rede ou outra instabilidade.

Nesse cenário, o cliente pode realizar uma nova tentativa da mesma operação.

Sem um mecanismo de idempotência, a repetição poderia resultar na criação de duas movimentações financeiras para uma única intenção do cliente.

Como o domínio trata de movimentações financeiras, duplicações desse tipo precisam ser evitadas.

## Decisão

Utilizar uma chave de idempotência fornecida pelo cliente através do header:

```text
Idempotency-Key
```

A chave será armazenada em uma tabela própria de idempotência juntamente com:

- a chave utilizada;
- o hash da requisição;
- o identificador do recurso criado;
- a data de criação.

A estrutura conceitual é:

```text
IdempotencyRecord
-----------------------------
Id
Key
RequestHash
ResourceId
CreatedAt
```

A coluna `Key` possui uma restrição de unicidade no banco de dados.

O registro de idempotência e a movimentação financeira são persistidos dentro da mesma transação.

Fluxo:

```text
Client
  |
  | POST + Idempotency-Key
  v
Write API
  |
  +----> verifica chave existente
  |
  | chave não encontrada
  v
Transaction + IdempotencyRecord
  |
  v
PostgreSQL
```

## Comportamento para requisições repetidas

Quando uma requisição utiliza uma chave que já existe, o sistema compara o hash da requisição atual com o hash armazenado.

### Mesma chave e mesma requisição

Quando a chave e o conteúdo da requisição são os mesmos, a operação é considerada uma repetição da solicitação original.

A API retorna o `ResourceId` originalmente criado, evitando a criação de uma nova movimentação.

```text
Request A
   |
   | Idempotency-Key = X
   v
Create Transaction
   |
   v
ResourceId = A
```

Repetição:

```text
Request A
   |
   | Idempotency-Key = X
   v
Existing IdempotencyRecord
   |
   v
ResourceId = A
```

### Mesma chave e requisição diferente

Quando a mesma chave é utilizada com dados diferentes, a operação é rejeitada.

Essa situação resulta em:

```text
HTTP 409 Conflict
```

A comparação é feita utilizando o hash dos dados relevantes da requisição.

## Hash da requisição

O hash é calculado a partir dos dados da operação.

Para a criação de uma movimentação, são considerados:

```text
AccountId
Description
Amount
Type
```

O hash é gerado utilizando SHA-256.

Isso permite verificar se uma chave existente está sendo reutilizada para a mesma operação ou para uma operação diferente.

## Garantia de unicidade

A aplicação realiza uma consulta inicial para verificar se a chave já existe.

Entretanto, essa verificação isoladamente não é suficiente para garantir a unicidade em cenários concorrentes.

Por isso, o banco de dados também possui uma restrição única para a chave de idempotência.

Em uma condição de corrida:

```text
Request A ----+
              |
              v
        PostgreSQL
              ^
              |
Request B ----+
```

somente uma das operações consegue inserir a mesma chave.

Quando ocorre uma violação da restrição de unicidade, a infraestrutura converte a exceção do banco em uma exceção de domínio específica de idempotência.

A aplicação então recupera o registro já criado e avalia o hash da requisição.

## Atomicidade

A movimentação financeira e o registro de idempotência são persistidos na mesma transação.

Conceitualmente:

```text
BEGIN TRANSACTION

    INSERT financial_transaction

    INSERT idempotency_record

COMMIT
```

Se qualquer uma das operações falhar:

```text
ROLLBACK
```

Isso evita situações em que exista uma movimentação financeira sem seu registro de idempotência correspondente ou um registro de idempotência sem a movimentação correspondente.

## Alternativas consideradas

### Não utilizar idempotência

A alternativa seria deixar o cliente controlar os retries sem qualquer mecanismo no servidor.

Essa abordagem não garante que uma nova tentativa represente uma operação diferente da anterior.

Para movimentações financeiras, esse comportamento poderia resultar em duplicidade.

Por esse motivo, essa alternativa não foi adotada.

### Utilizar apenas o identificador da transação

Outra possibilidade seria exigir que o cliente fornecesse diretamente um identificador único da movimentação.

Essa abordagem poderia evitar duplicidade por identificador, mas não forneceria o mesmo mecanismo explícito para distinguir uma repetição da mesma solicitação de uma tentativa de reutilizar o identificador com dados diferentes.

Foi adotada uma chave específica de idempotência separada do identificador da movimentação.

### Controlar a idempotência somente na aplicação

Uma alternativa seria realizar apenas uma consulta antes da inserção:

```text
SELECT
   |
   v
Existe?
   |
   v
INSERT
```

Essa abordagem possui uma condição de corrida quando duas requisições concorrentes consultam a chave antes que qualquer uma delas realize a inserção.

Por esse motivo, a garantia definitiva de unicidade é delegada ao banco de dados através da constraint única.

## Consequências

### Positivas

- Evita criação duplicada em retries.
- Permite que o cliente repita uma operação com segurança.
- A restrição única do banco protege contra condições de corrida.
- O hash impede reutilização inconsistente da mesma chave.
- O registro de idempotência e a movimentação são tratados atomicamente.
- O mecanismo é independente do identificador interno da movimentação.

### Negativas

- É necessário persistir informações adicionais para cada operação idempotente.
- O fluxo de criação possui consultas e tratamento adicionais.
- A aplicação precisa diferenciar repetição válida de reutilização inválida da chave.
- A tabela de idempotência precisa possuir uma política futura de retenção/limpeza caso o volume cresça significativamente.

## Relação com outras decisões

Esta decisão está diretamente relacionada à estratégia de consistência e transações da Write API.

Ela também complementa:

- [ADR-0001 — Separação entre Write API e Query API](ADR-0001-separacao-write-e-query.md)

A idempotência é aplicada exclusivamente ao fluxo de escrita, mantendo a Query API como uma API especializada em leitura.

## Status

Accepted.
