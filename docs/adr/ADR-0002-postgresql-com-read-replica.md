# ADR-0002 — PostgreSQL com Read Replica

- Status: Accepted
- Data: 2026-09-29

## Contexto

A arquitetura separa as operações de escrita das operações de consulta.

A Write API é responsável pelas alterações dos dados e utiliza o PostgreSQL Primary como destino das operações de escrita.

A Query API é responsável pelas consultas de saldo e possui uma carga de leitura diferente das operações de escrita.

O desafio exige que a solução considere alta demanda, instabilidade e falhas parciais. Nesse cenário, concentrar as operações de leitura e escrita na mesma instância do banco pode fazer com que consultas concorram diretamente com as operações responsáveis pela persistência.

Foi necessário definir uma estratégia para distribuir a carga de leitura sem retirar da Write API a responsabilidade sobre o banco principal.

## Decisão

Utilizar PostgreSQL com uma instância Primary e uma Read Replica através de streaming replication.

O fluxo de persistência será:

```text
                 Write API
                    |
                    v
          +-------------------+
          | PostgreSQL Primary |
          +---------+---------+
                    |
                    | Streaming Replication
                    v
          +-------------------+
          | PostgreSQL        |
          | Read Replica      |
          +---------+---------+
                    |
                    v
                Query API
```

A Write API realiza as operações de escrita exclusivamente no PostgreSQL Primary.

A Query API realiza suas consultas no PostgreSQL Read Replica.

A Query API possui uma connection string específica para o banco de leitura:

```text
ConnectionStrings:ReadDatabase
```

## Motivações

### Separação da carga de leitura

As consultas realizadas pela Query API não precisam utilizar diretamente a mesma instância responsável pelas operações de escrita.

Isso permite distribuir parte da carga do banco para a réplica de leitura.

### Escalabilidade

A arquitetura permite aumentar a capacidade de leitura sem alterar o fluxo de escrita.

Caso a demanda de consultas aumente, novas réplicas de leitura podem ser consideradas no futuro, mantendo o PostgreSQL Primary como origem dos dados.

### Isolamento entre leitura e escrita

A Write API mantém sua responsabilidade sobre o banco principal.

A Query API não realiza operações de alteração no banco de leitura.

Essa separação reduz o acoplamento entre os dois fluxos.

### Compatibilidade com a separação dos microserviços

A decisão complementa o ADR-0001.

A separação entre Write API e Query API permite que cada serviço utilize o banco mais adequado ao seu respectivo fluxo:

```text
Write API
    |
    v
Primary

Query API
    |
    v
Read Replica
```

## Consistência

A utilização de uma réplica de leitura implica replicação assíncrona entre o Primary e a Read Replica.

Consequentemente, pode existir um pequeno intervalo entre a confirmação de uma escrita no Primary e a disponibilidade dessa alteração na Read Replica.

Por esse motivo, a Query API pode apresentar temporariamente um estado anterior ao mais recente enquanto a replicação não for concluída.

Essa característica é aceita pela arquitetura para permitir a separação da carga de leitura.

## Alternativas consideradas

### Utilizar apenas o PostgreSQL Primary

Uma alternativa seria direcionar tanto as operações de escrita quanto as consultas para a mesma instância PostgreSQL.

```text
Write API ----+
              |
              v
         PostgreSQL
              ^
              |
Query API ----+
```

Essa abordagem seria mais simples de operar e não introduziria atraso de replicação.

Entretanto, todas as consultas competiriam diretamente pelos mesmos recursos utilizados pelas operações de escrita.

Por esse motivo, essa alternativa não foi adotada.

### Utilizar um banco separado para consultas sem replicação

Outra alternativa seria manter um banco de leitura independente e atualizar seus dados através de um processo específico de sincronização.

Essa abordagem permitiria maior liberdade para modelar os dados de consulta, mas introduziria uma infraestrutura adicional de sincronização e maior complexidade operacional.

Para o escopo atual, a replicação nativa do PostgreSQL atende ao objetivo com menor complexidade.

## Consequências

### Positivas

- Distribuição da carga de leitura.
- Preservação do PostgreSQL Primary como origem das escritas.
- Isolamento entre operações de leitura e escrita.
- Possibilidade de adicionar novas réplicas de leitura futuramente.
- Aproveitamento dos mecanismos nativos de replicação do PostgreSQL.
- Compatibilidade direta com a separação entre Write API e Query API.

### Negativas

- Existe possibilidade de atraso entre Primary e Read Replica.
- A infraestrutura local possui mais de uma instância PostgreSQL.
- O ambiente possui maior complexidade operacional que uma instalação PostgreSQL única.
- É necessário monitorar o estado da replicação.
- Consultas que exigem leitura imediatamente após uma escrita podem observar temporariamente dados ainda não replicados.

## Falha da Read Replica

A indisponibilidade da Read Replica não deve comprometer diretamente a capacidade da Write API de registrar movimentações no Primary.

A Query API, entretanto, ficará indisponível para consultas enquanto não houver uma fonte de leitura disponível.

A arquitetura permite que, futuramente, outras réplicas sejam adicionadas para reduzir esse ponto de falha.

## Relação com outras decisões

Esta decisão depende da separação definida em:

- [ADR-0001 — Separação entre Write API e Query API](ADR-0001-separacao-write-e-query.md)

E influencia:

- [ADR-0005 — Query Service com Dapper](ADR-0005-query-service-com-dapper.md)

## Status

Accepted.
