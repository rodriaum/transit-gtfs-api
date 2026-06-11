# Multi-stage build

FROM gradle:8.5-jdk21 AS build
WORKDIR /app

COPY build.gradle settings.gradle ./
COPY gradle ./gradle

RUN gradle dependencies --no-daemon

COPY src ./src

RUN gradle clean build -x test --no-daemon

FROM eclipse-temurin:21-jre-alpine

WORKDIR /app

RUN addgroup -S tranzor && adduser -S tranzor -G tranzor

COPY --from=build /app/build/libs/*.jar app.jar

RUN mkdir -p /data/gtfs /data/otp /tmp/gtfs && \
    chown -R tranzor:tranzor /data /tmp/gtfs

USER tranzor

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=60s \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/v1/tranzor/actuator/health || exit 1

ENTRYPOINT ["java", "-jar", "/app/app.jar"]
