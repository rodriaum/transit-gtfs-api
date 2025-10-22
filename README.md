# Tranzor - API

Tranzor API (ASP.NET) permite consultar horários, paradas, viagens e próximas partidas de itinerários.

<!--
## Docs

Você pode acessar os documentos da API por [aqui](https://metro-porto.gitbook.io/metro-porto)
-->

## Primeiros Passos

Antes de iniciar o projeto pela primeira vez, siga os passos abaixo:

1. Configure o arquivo `.env` com as variáveis de ambiente necessárias.
2. Acesse o banco de dados PostgreSQL e execute o seguinte comando para habilitar o suporte a dados geoespaciais:

   ```sql
   CREATE EXTENSION postgis;
   ```

3. Após isso, execute o comando para aplicar as migrações do Entity Framework:

   ```bash
   dotnet ef database update
   ```

4. Após rodar o comando acima, é necessário executar o arquivo [SQL/cities.sql](https://github.com/rodriaum/tranzor-api/tree/dev/SQL) diretamente no banco de dados. 
Este arquivo contém dados geográficos grandes (colunas `geom`) das zonas das cidades, por isso, não deve ser aberto e copiado manualmente.

## Arquitetura Técnica

- **Framework**: ASP.NET Core (.NET 9.0)
- **Base de Dados**: PostgreSQL + PostGIS, Apache Cassandra e Redis

## Funcionalidades

- **Partidas**: Obtenha informações de partidas para qualquer paragem
- **Planeamento de Rotas**: Aceda a informações completas de rotas e viagens para planeamento de percursos
- **Dados Geográficos**: Recupere dados de coordenadas para mapeamento e serviços de localização, agora com suporte a queries espaciais
- **Informações de Horários**: Aceda a horários detalhados e calendários de serviços
- **Cálculo de Tarifas**: Obtenha informações de preços e regras tarifárias
- **Informações de Transferência**: Encontre pontos de ligação entre diferentes linhas
- **PostGIS**: As tabelas `gtfs_stops` e `gtfs_shapes` possuem colunas geográficas (`location` e `geom`) usando tipos `geography (point)` e `geometry (point)` do PostGIS, permitindo consultas espaciais e integração avançada com mapas.

## Processamento de Dados

A API processa dados GTFS originais e converte-os para um formato otimizado armazenado no PostgreSQL. O Redis é utilizado para cache de consultas frequentes, garantindo tempos de resposta rápidos. O sistema inclui funcionalidades de recarregamento de dados para atualizações periódicas das informações de trânsito.

## Padrões de Dados

Esta API segue os padrões GTFS (General Transit Feed Specification), garantindo compatibilidade com aplicações de trânsito e fornecendo formatos de dados padronizados para informações de transporte público.

## Informações Técnicas

- **URL Base**: `/api/v1/tranzor`
- **Formatos Suportados**: JSON, Texto Simples
- **OpenTripPlanner**: Precisa configurar o [OTP](https://github.com/opentripplanner/OpenTripPlanner) para planejar rotas.
- **Paragem por Cidade**: Para conseguir procurar paragens por cidade precisa importar o [cities.sql](https://github.com/rodriaum/tranzor-api/blob/dev/SQL/cities.sql)

## Licença
[MIT License](https://github.com/rodriaum/tranzor-api?tab=MIT-1-ov-file#MIT-1-ov-file)