# Tranzor API

API de transportes públicos baseada em GTFS.

## Funcionalidades

- Próximas partidas por paragem
- Detalhes de linhas e rotas
- Horários completos
- Viagens ativas
- Planeamento de rotas com OTP
- Paragens próximas (PostGIS)
- Importação automática de GTFS
- Suporte multi agência

## Setup

### Local

```bash
git clone https://github.com/your-org/tranzor-api.git
cd tranzor-api
cp .env.example .env
```

### Docker

```bash
docker build -t tranzor-api .
docker run -p 8080:8080 tranzor-api
```

## GTFS

Configuração em:

```bash
/data/agencies.json
```
