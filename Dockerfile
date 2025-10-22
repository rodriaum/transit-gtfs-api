# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apk add --no-cache icu-libs

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Tranzor.csproj", "src/"]
RUN dotnet restore "src/Tranzor.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "Tranzor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Tranzor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY Config /app/Config

# Criar usuário não-root para segurança
RUN addgroup -g 1000 tranzor && \
    adduser -D -u 1000 -G tranzor tranzor && \
    chown -R tranzor:tranzor /app

RUN mkdir -p /app/Data && \
    chown -R tranzor:tranzor /app/Data

RUN chmod -R 755 /app/Data

USER tranzor

ENTRYPOINT ["dotnet", "Tranzor.dll"]