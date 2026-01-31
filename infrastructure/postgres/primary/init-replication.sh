#!/bin/bash
# =============================================================================
# PostgreSQL Primary - Initialize Replication Configuration
# =============================================================================
set -e

echo "=== Configuring PostgreSQL Primary for Replication ==="

# Create replication user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create replication user with replication privileges
    CREATE USER ${POSTGRES_REPLICATION_USER} WITH REPLICATION ENCRYPTED PASSWORD '${POSTGRES_REPLICATION_PASSWORD}';
    
    -- Grant necessary permissions
    GRANT ALL PRIVILEGES ON DATABASE ${POSTGRES_DB} TO ${POSTGRES_REPLICATION_USER};
    
    SELECT 'Replication user created successfully' as status;
EOSQL

# Configure postgresql.conf for replication
cat >> "$PGDATA/postgresql.conf" <<EOF

# =============================================================================
# Replication Configuration (added by init-replication.sh)
# =============================================================================
wal_level = replica
max_wal_senders = 3
max_replication_slots = 3
hot_standby = on
hot_standby_feedback = on
synchronous_commit = on
EOF

# Configure pg_hba.conf to allow replication connections
cat >> "$PGDATA/pg_hba.conf" <<EOF

# =============================================================================
# Replication Access (added by init-replication.sh)
# =============================================================================
# Allow replication connections from Docker network
host    replication     ${POSTGRES_REPLICATION_USER}    0.0.0.0/0    md5
host    all             all                              0.0.0.0/0    md5
EOF

echo "=== PostgreSQL Primary Replication Configuration Complete ==="
