# =============================================================================
# PostgreSQL Replica Server (Hot Standby)
# =============================================================================
FROM postgres:16-alpine

LABEL maintainer="DBH-EHR Team"
LABEL description="PostgreSQL Replica with streaming replication from primary"

# Copy initialization script
COPY init-replica.sh /init-replica.sh

# Make script executable
RUN chmod +x /init-replica.sh

# Override entrypoint to set up replica
ENTRYPOINT ["/init-replica.sh"]

# Expose PostgreSQL port
EXPOSE 5432
