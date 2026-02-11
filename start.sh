#!/bin/bash

# Tranzor API - Quick Start Script

echo "🚀 Starting Tranzor API..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Start infrastructure services
echo "📦 Starting infrastructure services (PostgreSQL, Redis, Cassandra, OTP)..."
docker-compose up -d postgres redis cassandra otp

echo "⏳ Waiting for services to be ready..."
sleep 10

# Check PostgreSQL
echo "🔍 Checking PostgreSQL..."
until docker exec tranzor-postgres pg_isready -U tranzor > /dev/null 2>&1; do
    echo "   Waiting for PostgreSQL..."
    sleep 2
done
echo "✅ PostgreSQL is ready"

# Check Redis
echo "🔍 Checking Redis..."
until docker exec tranzor-redis redis-cli ping > /dev/null 2>&1; do
    echo "   Waiting for Redis..."
    sleep 2
done
echo "✅ Redis is ready"

# Build and run the application
echo "🔨 Building Tranzor API..."
./gradlew clean build -x test

echo "🚀 Starting Tranzor API..."
./gradlew bootRun &

# Wait for API to be ready
echo "⏳ Waiting for API to start..."
sleep 15

# Check API health
until curl -f http://localhost:8080/api/v1/tranzor/actuator/health > /dev/null 2>&1; do
    echo "   Waiting for API..."
    sleep 3
done

echo ""
echo "✅ Tranzor API is running!"
echo ""
echo "📚 Access the API documentation at:"
echo "   http://localhost:8080/api/v1/tranzor/swagger-ui.html"
echo ""
echo "🔧 Additional services:"
echo "   PgAdmin: http://localhost:5050 (admin@tranzor.com / admin)"
echo "   Redis Commander: http://localhost:8082"
echo ""
echo "📊 Example endpoints:"
echo "   GET /stops/nearby?latitude=41.1496&longitude=-8.6109&radiusMeters=500"
echo "   GET /departures?stopId=STOP123&agencyId=metro_porto&limit=10"
echo "   GET /routes?agencyId=metro_porto"
echo ""
