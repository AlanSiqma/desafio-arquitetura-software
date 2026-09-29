#!/bin/bash

set -e

echo "Criando usuário de replicação..."

psql -v ON_ERROR_STOP=1 \
  --username "$POSTGRES_USER" \
  --dbname "$POSTGRES_DB" <<-EOSQL

  CREATE ROLE replication
    WITH REPLICATION
    LOGIN
    PASSWORD 'replication';

EOSQL

echo "Configurando pg_hba.conf..."

echo "host replication replication 0.0.0.0/0 scram-sha-256" \
  >> "$PGDATA/pg_hba.conf"

echo "Configuração concluída."