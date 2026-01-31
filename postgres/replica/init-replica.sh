#!/bin/bash
# =============================================================================
# PostgreSQL Replica - Initialize from Primary via Streaming Replication
# =============================================================================
set -e

echo "=== Setting up PostgreSQL Replica ==="

# Wait for primary to be ready
until PGPASSWORD=${POSTGRES_REPLICATION_PASSWORD} pg_isready -h ${POSTGRES_PRIMARY_HOST} -p 5432 -U ${POSTGRES_REPLICATION_USER}
do
    echo "Waiting for primary to be ready..."
    sleep 2
done

echo "Primary is ready. Checking if replica needs initialization..."

# Check if data directory is empty (needs base backup)
if [ -z "$(ls -A $PGDATA 2>/dev/null)" ]; then
    echo "Data directory is empty. Performing base backup from primary..."
    
    # Perform base backup from primary
    PGPASSWORD=${POSTGRES_REPLICATION_PASSWORD} pg_basebackup \
        -h ${POSTGRES_PRIMARY_HOST} \
        -p 5432 \
        -U ${POSTGRES_REPLICATION_USER} \
        -D ${PGDATA} \
        -Fp \
        -Xs \
        -P \
        -R
    
    echo "Base backup completed."
    
    # Configure replica settings
    cat >> "$PGDATA/postgresql.conf" <<EOF

# =============================================================================
# Replica Configuration (added by init-replica.sh)
# =============================================================================
hot_standby = on
hot_standby_feedback = on
EOF

    # Set correct permissions
    chmod 700 ${PGDATA}
    
    echo "Replica configuration completed."
else
    echo "Data directory already initialized. Starting replica..."
fi

echo "=== Starting PostgreSQL Replica ==="

# Start PostgreSQL
exec postgres
