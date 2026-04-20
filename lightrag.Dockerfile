FROM python:3.10-slim

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install python dependencies (CPU versions for speed)
RUN pip install torch --index-url https://download.pytorch.org/whl/cpu
RUN pip install "lightrag-hku[api]" sentence-transformers numpy

# Pre-download the embedding model to cache it in the image
RUN python -c "from sentence_transformers import SentenceTransformer; SentenceTransformer('all-MiniLM-L6-v2')"

EXPOSE 9621

# The command will be overridden in docker-compose for the indexing script
CMD ["lightrag-server", "--host", "0.0.0.0", "--port", "9621"]
