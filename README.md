# Tranzor API - Public Transport API

API completa de transportes públicos para Portugal, baseada em dados GTFS.

## 🚀 Funcionalidades

- ✅ Consultar próximas partidas por paragem
- ✅ Consultar detalhes de rotas/linhas
- ✅ Consultar viagens ativas
- ✅ Consultar horários completos
- ✅ Calcular rotas entre origem e destino (integração OTP)
- ✅ Procurar paragens próximas com PostGIS
- ✅ Suporte a queries espaciais
- ✅ Sistema de cache com Redis
- ✅ Processamento automático de ficheiros GTFS
- ✅ Suporte a múltiplas agências de transporte

## 🛠️ Stack Tecnológica

- **Java 21**
- **Spring Boot 3.2**
- **PostgreSQL 16 + PostGIS**
- **Redis** (cache)
- **Apache Cassandra** (dados históricos)
- **OpenTripPlanner** (planeamento de rotas)
- **Gradle**
- **Flyway** (migrações)
- **Swagger/OpenAPI** (documentação)
- **Docker & Docker Compose**

## 📋 Requisitos

- Java 21+
- Docker & Docker Compose
- Gradle 8+

## 🚀 Quick Start

### 1. Clonar o repositório

```bash
git clone https://github.com/your-org/tranzor-api.git
cd tranzor-api
```

### 2. Configurar variáveis de ambiente

```bash
# Copiar template de configuração
cp .env.example .env

# Editar credenciais (opcional para desenvolvimento)
nano .env
```

⚠️ **Importante**: O arquivo `.env` contém credenciais sensíveis. **NUNCA** o commite no Git!
Ver guia completo em [ENV_CONFIG.md](ENV_CONFIG.md)

### 3. Iniciar serviços com Docker Compose

```bash
docker-compose up -d
```

Isto irá iniciar:
- PostgreSQL com PostGIS (porta 5432)
- Redis (porta 6379)
- Cassandra (porta 9042)
- OpenTripPlanner (porta 8081)
- PgAdmin (porta 5050)
- Redis Commander (porta 8082)

### 3. Compilar a aplicação

```bash
./gradlew clean build
```

### 4. Executar a aplicação

```bash
./gradlew bootRun
```

A API estará disponível em: `http://localhost:8080/api/v1/tranzor`

## 📚 Documentação da API

Aceda à documentação Swagger em:
```
http://localhost:8080/api/v1/tranzor/swagger-ui.html
```

## 🔌 Endpoints Principais

### Paragens (Stops)

```http
GET /stops/{stopId}?agencyId={agencyId}
GET /stops/nearby?latitude=41.1496&longitude=-8.6109&radiusMeters=500
GET /stops/search?q=bolhão
GET /stops?agencyId=metro_porto
```

### Partidas (Departures)

```http
GET /departures?stopId=STOP123&agencyId=metro_porto&limit=10
GET /departures/by-route?stopId=STOP123&routeId=ROUTE1&agencyId=metro_porto
GET /departures/schedule?stopId=STOP123&agencyId=metro_porto&date=2026-02-11
```

### Rotas (Routes)

```http
GET /routes/{routeId}?agencyId=metro_porto
GET /routes?agencyId=metro_porto
GET /routes/by-type?routeType=1&agencyId=metro_porto
GET /routes/search?q=azul
```

### Planeamento de Viagem (Trip Planner)

```http
GET /trip-planner/plan?fromLat=41.1496&fromLon=-8.6109&toLat=41.1579&toLon=-8.6291
```

## 🗄️ Estrutura de Dados

### Ficheiro de Configuração de Agências

Criar `/data/agencies.json`:

```json
[
    {
        "agency_id": "metro_porto",
        "name": "Metro do Porto",
        "image_url": "",
        "gtfs_url": "https://www.metrodoporto.pt/.../gtfs.zip",
        "gtfs_expire_at": "",
        "realtime_urls": {
            "VehiclePositions": {
                "realtime_file_type": "WebSocket",
                "url": "wss://mmt.portodigital.pt/websocket"
            }
        }
    }
]
```

## 🔧 Configuração

### Variáveis de Ambiente

Pode configurar através de variáveis de ambiente:

```bash
export SPRING_DATASOURCE_URL=jdbc:postgresql://localhost:5432/tranzor
export SPRING_DATASOURCE_USERNAME=tranzor
export SPRING_DATASOURCE_PASSWORD=tranzor123
export SPRING_DATA_REDIS_HOST=localhost
export SPRING_DATA_REDIS_PORT=6379
```

### application.yml

Configuração principal em `src/main/resources/application.yml`

## 📊 PostGIS - Queries Espaciais

A API usa PostGIS para queries geográficas eficientes:

```sql
-- Encontrar paragens num raio de 500 metros
SELECT * FROM stops 
WHERE ST_DWithin(
    location::geography,
    ST_MakePoint(-8.6109, 41.1496)::geography,
    500
);
```

## 🔄 Importação de Dados GTFS

A importação de GTFS pode ser feita:

1. **Automaticamente** - na inicialização da aplicação
2. **Manualmente** - através de endpoint específico
3. **Agendado** - usando scheduled tasks

## 🧪 Testes

```bash
# Executar todos os testes
./gradlew test

# Executar testes de integração
./gradlew integrationTest
```

## 🐳 Docker

### Build da imagem

```bash
docker build -t tranzor-api .
```

### Executar com Docker

```bash
docker run -p 8080:8080 \
  -e SPRING_DATASOURCE_URL=jdbc:postgresql://postgres:5432/tranzor \
  -e SPRING_DATA_REDIS_HOST=redis \
  tranzor-api
```

## 📈 Performance

- **Cache Redis** - reduz queries ao database
- **PostGIS** - queries geográficas ultra-rápidas
- **Índices otimizados** - nas tabelas principais
- **Cassandra** - para dados históricos de grande volume
- **Connection pooling** - HikariCP

## 🔒 Segurança

- JWT Authentication (configurável)
- Rate Limiting
- CORS configurável
- Input validation

## 📝 Logs

Os logs são estruturados e podem ser vistos em:
```
logs/tranzor.log
```