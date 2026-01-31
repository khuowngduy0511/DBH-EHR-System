# =============================================================================
# PostgreSQL Primary Server with Replication Support
# =============================================================================
FROM postgres:16-alpine

LABEL maintainer="DBH-EHR Team"
LABEL description="PostgreSQL Primary with streaming replication enabled"

# Copy initialization script
COPY init-replication.sh /docker-entrypoint-initdb.d/00-init-replication.sh

# Make script executable
RUN chmod +x /docker-entrypoint-initdb.d/00-init-replication.sh

# Expose PostgreSQL port
EXPOSE 5432
