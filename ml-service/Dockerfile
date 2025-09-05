# Use Python 3.11 slim image (better package compatibility)
FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Set environment variables
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1
ENV PYTHONPATH=/app
ENV PIP_NO_CACHE_DIR=1
ENV PIP_DISABLE_PIP_VERSION_CHECK=1

# Install system dependencies for faster builds
RUN apt-get update && apt-get install -y \
    gcc \
    g++ \
    build-essential \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Upgrade pip and install wheel for faster package installation
RUN pip install --upgrade pip setuptools wheel

# Copy requirements first for better Docker layer caching
COPY requirements.txt .

# Install Python dependencies with optimizations
RUN pip install --no-cache-dir --prefer-binary -r requirements.txt

# Copy application code
COPY . .

# Create necessary directories
RUN mkdir -p models data outputs logs

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
    CMD python -c "import requests; requests.get('http://localhost:8000/health', timeout=10)"

# Run the application
CMD ["python", "main.py"]
