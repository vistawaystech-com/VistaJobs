#!/bin/bash
set -e

# Always run from the script's own directory (VistaJobs root), no matter where it's called from
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
echo "Working directory: $(pwd)"

echo "Pulling latest code..."
git pull origin dev

echo "Ensuring SQL Server is running..."
docker start sqlserver 2>/dev/null || echo "  (sqlserver container not found or already running)"

echo "Stopping old app container..."
docker stop vistajobs-test 2>/dev/null
docker rm vistajobs-test 2>/dev/null

echo "Building new image..."
docker build -t vistajobs:latest -f JBP/Dockerfile .

echo "Starting new container..."
docker run -d -p 9090:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=JBPDB;User Id=sa;Password=VistaJobs@2026;TrustServerCertificate=True" \
  --restart unless-stopped \
  --name vistajobs-test vistajobs:latest

docker update --restart unless-stopped sqlserver 2>/dev/null

echo "✅ Done! App running at http://localhost:9090/swagger"
