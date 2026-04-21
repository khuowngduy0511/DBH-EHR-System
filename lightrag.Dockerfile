FROM python:3.10-slim

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install python dependencies (CPU versions for speed)
RUN pip install torch --index-url https://download.pytorch.org/whl/cpu
RUN pip install "lightrag-hku[api]" numpy openai asyncpg

# Pre-download step omitted: DBH System now uses OpenRouter API for embeddings.

EXPOSE 9621

# The command will be overridden in docker-compose for the indexing script
CMD ["lightrag-server", "--host", "0.0.0.0", "--port", "9621"]
