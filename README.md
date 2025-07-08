# Transit GTFS - API

Transit GTFS API (ASP.NET) permite consultar horários, paradas, viagens e próximas partidas de itinerários.

<!--
## Docs

Você pode acessar os documentos da API por [aqui](https://metro-porto.gitbook.io/metro-porto)
-->

## Arquitetura Técnica

- **Framework**: ASP.NET Core (.NET 9.0)
- **Base de Dados**: PostgreSQL + PostGIS
- **Cache**: Redis

## Categorias

### 🏢 Informações da Agência
Fornece informações sobre as agências de transporte que operam, incluindo detalhes da agência, informações de contacto e dados operacionais.

### 📅 Calendários e Horários
Gere informações do calendário de serviços que define quando os serviços operam. Isto inclui horários semanais regulares, exceções de feriados e datas de serviços especiais que afetam as operações normais.

### 🚇 Rotas e Linhas
Contém informações sobre rotas e linhas, incluindo nomes das rotas, cores, descrições e dados de viagens associadas. Permite acesso a informações completas da rota com todas as viagens relacionadas.

### 🚉 Paragens e Estações
Fornece dados abrangentes sobre paragens e estações, incluindo coordenadas de localização, nomes, códigos e informações de partida em tempo real. Essencial para encontrar estações próximas e obter horários de partida ao vivo.

### 🚊 Viagens e Percursos
Gere dados de viagens individuais que representam percursos específicos ao longo das rotas. Inclui horários de viagens, destinos e informações detalhadas de tempo paragem a paragem.

### ⏰ Horários de Paragens
Trata informações detalhadas de horários sobre quando os itinerários chegam e partem de cada paragem. Suporta paginação para grandes conjuntos de dados e fornece dados de tempo específicos da viagem.

### 🗺️ Formas Geográficas
Contém dados de coordenadas geográficas que definem os caminhos físicos das rotas nos mapas. Essencial para exibir linhas de rota. Agora com suporte a queries espaciais via PostGIS.

### 🔄 Transferências e Ligações
Gere pontos de transferência entre diferentes linhas e rotas, ajudando os utilizadores a planear viagens multi-linha e compreender possibilidades de ligação em toda a rede.

### 💰 Informações Tarifárias
Fornece informações de preços e regras tarifárias para viagens, incluindo diferentes tipos de tarifas, métodos de pagamento, políticas de transferência e estruturas de preços baseadas em zonas.

### 🆔 Identificadores e Traduções
Inclui serviços para traduções de campos (Translation), informações de feeds (FeedInfo), atribuições (Attribution) e áreas de paragem (StopArea).

### 🏷️ Produtos, Mídias e Regras de Tarifa
Gerencia produtos tarifários (FareProduct), mídias de pagamento (FareMedia), regras de tarifa por trecho (FareLegRule) e redes (Network).

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

## Casos de Uso

- **Aplicações Móveis de Trânsito**: Construa aplicações que mostram partidas em tempo real e planeamento de rotas
- **Serviços de Mapeamento Web**: Exiba rotas e paragens em mapas interativos
- **Planeadores de Viagem**: Crie ferramentas de planeamento de viagens com informações de transferência
- **Calculadoras de Tarifas**: Desenvolva ferramentas de estimativa de tarifas para viagens
- **Análise de Dados**: Analise padrões de uso e performance do serviço

## Informações Técnicas

- **URL Base**: `/api/v1/transit/gtfs`
- **Formatos Suportados**: JSON, Texto Simples
- **Autenticação**: Não requerida para endpoints públicos
- **Rate Limiting**: Recomenda-se uso responsável com implementação de cache local

## Licença
[MIT License](https://github.com/rodriaum/transit-gtfs-api?tab=MIT-1-ov-file#MIT-1-ov-file)