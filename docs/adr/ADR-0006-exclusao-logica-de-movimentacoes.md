# ADR-0006 — Exclusão Lógica de Movimentações

- Status: Accepted
- Data: 2026-09-29

## Contexto

A Write API disponibiliza a operação de exclusão de uma movimentação financeira através do endpoint:

```text
DELETE /api/transactions/{id}
```

As movimentações financeiras representam registros persistidos do sistema e podem participar do cálculo do saldo de uma conta.

Uma exclusão física removeria o registro da base de dados e dificultaria a preservação do histórico da operação.

Além disso, a aplicação precisa manter o registro físico disponível para rastreabilidade e para permitir que mecanismos de persistência e auditoria sejam evoluídos posteriormente.

## Decisão

Utilizar exclusão lógica (soft delete) para as movimentações financeiras.

A entidade `FinancialTransaction` possui os campos:

```text
IsDeleted
DeletedAt
```

Quando uma movimentação é excluída, o registro não é removido fisicamente do banco.

Em vez disso:

```text
IsDeleted = true
DeletedAt = data/hora da exclusão
UpdatedAt = data/hora da exclusão
Version   = nova versão
```

O método de domínio responsável pela operação é:

```text
Delete()
```

Uma movimentação já excluída não pode ser atualizada.

## Filtragem automática

O Entity Framework Core utiliza um query filter para impedir que registros excluídos sejam retornados normalmente:

```csharp
builder.HasQueryFilter(x => !x.IsDeleted);
```

Dessa forma, as consultas da Write API que utilizam o Entity Framework Core trabalham, por padrão, somente com movimentações ativas.

A Query API também aplica explicitamente o filtro:

```sql
WHERE is_deleted = false
```

Isso mantém o comportamento consistente entre os dois serviços.

## Motivações

### Preservação do registro

A exclusão lógica mantém o registro original no banco de dados.

Isso permite preservar informações como:

- identificador da movimentação;
- data de criação;
- data da exclusão;
- versão;
- demais dados originalmente persistidos.

### Rastreabilidade

A aplicação mantém informações que indicam que o registro foi excluído, em vez de simplesmente remover sua existência do banco.

Isso fornece uma base melhor para futuras necessidades de auditoria ou histórico.

### Integridade referencial

A manutenção física do registro evita que outras informações que eventualmente dependam do identificador da movimentação sejam afetadas por uma exclusão física.

### Compatibilidade com a consulta de saldo

Movimentações excluídas não devem participar do cálculo do saldo atual realizado pela Query API.

Por isso, a consulta utiliza:

```sql
is_deleted = false
```

## Alternativas consideradas

### Exclusão física

A alternativa seria executar um `DELETE` diretamente no banco.

Essa abordagem removeria permanentemente o registro.

Embora seja mais simples, ela perderia os dados persistidos da movimentação e dificultaria a rastreabilidade da operação.

Por esse motivo, não foi adotada.

### Manter o registro sem flag de exclusão

Outra alternativa seria manter todas as movimentações no banco e utilizar algum mecanismo externo para determinar quais registros deveriam participar das consultas.

Essa abordagem aumentaria a complexidade da consulta e não deixaria o estado de exclusão explicitamente representado na entidade.

Por esse motivo, foi adotada a representação explícita através de `IsDeleted` e `DeletedAt`.

## Consequências

### Positivas

- Preserva fisicamente os registros excluídos.
- Mantém informações sobre quando a exclusão ocorreu.
- Facilita rastreabilidade.
- Evita perda imediata dos dados.
- Permite implementar auditoria ou histórico mais completo futuramente.
- Mantém a operação de exclusão compatível com o controle de versão e concorrência.

### Negativas

- O banco continua armazenando registros excluídos.
- As consultas precisam considerar o estado `IsDeleted`.
- É necessário definir futuramente uma política de retenção caso o volume de registros excluídos cresça significativamente.
- A exclusão lógica não representa, por si só, um histórico completo de todas as versões da movimentação.

## Consideração sobre histórico temporal

A exclusão lógica preserva o registro e a informação de exclusão, mas não cria automaticamente uma versão histórica completa da movimentação.

Por exemplo, alterações realizadas antes da exclusão continuam representadas pelo estado atual da entidade.

Caso o sistema passe a exigir reconstrução exata do saldo em qualquer instante histórico, será necessário adotar uma estratégia adicional de versionamento ou histórico das movimentações.

Essa decisão não implementa esse mecanismo.

## Relação com outras decisões

Esta decisão está relacionada a:

- [ADR-0004 — Controle de Concorrência Otimista](ADR-0004-controle-de-concorrencia-otimista.md)
- [ADR-0005 — Query Service com Dapper](ADR-0005-query-service-com-dapper.md)

A exclusão lógica atualiza a versão da entidade e a Query API desconsidera movimentações marcadas como excluídas.

## Status

Accepted.
