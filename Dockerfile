# Multi-stage build

# Stage 1: Build
FROM gradle:8.5-jdk21 AS build
WORKDIR /app

# Copy gradle files
COPY build.gradle settings.gradle ./
COPY gradle ./gradle

# Download dependencies
RUN gradle dependencies --no-daemon

# Copy source code
COPY src ./src

# Build application
RUN gradle clean build -x test --no-daemon

# Stage 2: Runtime
FROM eclipse-temurin:21-jre-alpine

WORKDIR /app

# Create non-root user
RUN addgroup -S tranzor && adduser -S tranzor -G tranzor

# Copy jar from build stage
COPY --from=build /app/build/libs/*.jar app.jar

# Create directories for data
RUN mkdir -p /data/gtfs /data/otp /tmp/gtfs && \
    chown -R tranzor:tranzor /data /tmp/gtfs

# Switch to non-root user
USER tranzor

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/v1/tranzor/actuator/health || exit 1

# Run application
ENTRYPOINT ["java", "-jar", "/app/app.jar"]
