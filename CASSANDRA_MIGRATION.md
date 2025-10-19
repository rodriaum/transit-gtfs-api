# Migração StopTimes para Cassandra

## Mudanças Implementadas

Os dados de `StopTimes` foram migrados do PostgreSQL para o Apache Cassandra para melhor desempenho e escalabilidade.

## Arquivos Modificados

### Novos Arquivos
- `src/Interfaces/Database/ICassandraService.cs` - Interface para serviço Cassandra
- `src/Services/Database/CassandraService.cs` - Implementação do serviço Cassandra

### Arquivos Modificados
- `src/Models/Gtfs/StopTime.cs` - Removidos atributos `[NotMapped]`
- `src/Databases/GTFSContext.cs` - Removido `DbSet<StopTime>`
- `src/Services/Gtfs/Static/StopTimesService.cs` - Atualizado para usar Cassandra
- `src/Startup.cs` - Adicionado registro e inicialização do Cassandra
- `src/Constant.cs` - Adicionadas variáveis de ambiente do Cassandra
- `src/Tranzor.csproj` - Adicionado pacote `CassandraCSharpDriver`

## Configuração

### Variáveis de Ambiente Necessárias

Adicione as seguintes variáveis ao seu arquivo `.env`:

```env
CASSANDRA_CONTACT_POINTS=localhost
CASSANDRA_KEYSPACE=tranzor
CASSANDRA_USERNAME=cassandra
CASSANDRA_PASSWORD=cassandra
```

**Notas:**
- `CASSANDRA_CONTACT_POINTS`: Lista de hosts Cassandra separados por vírgula (ex: `host1,host2,host3`)
- `CASSANDRA_KEYSPACE`: Nome do keyspace onde os dados serão armazenados
- `CASSANDRA_USERNAME` e `CASSANDRA_PASSWORD`: Credenciais (opcionais se Cassandra não usar autenticação)

### Estrutura da Tabela Cassandra

A tabela `stop_times` é criada automaticamente na inicialização com a seguinte estrutura:

```cql
CREATE TABLE stop_times (
    trip_id text,
    stop_sequence int,
    id text,
    arrival_time text,
    departure_time text,
    stop_id text,
    stop_headsign text,
    pickup_type int,
    drop_off_type int,
    shape_dist_traveled double,
    timepoint int,
    PRIMARY KEY ((trip_id), stop_sequence)
) WITH CLUSTERING ORDER BY (stop_sequence ASC);

CREATE INDEX stop_times_stop_id_idx ON stop_times (stop_id);
```

### Instalação do Cassandra

#### Docker (Recomendado)
```bash
docker run -d --name cassandra \
  -p 9042:9042 \
  -e CASSANDRA_CLUSTER_NAME=tranzor-cluster \
  cassandra:latest
```

#### Local
Baixe e instale o Apache Cassandra de: https://cassandra.apache.org/download/

## Comportamento

### Inicialização
1. O serviço Cassandra é inicializado no startup da aplicação
2. O keyspace é criado automaticamente se não existir
3. A tabela `stop_times` e seus índices são criados automaticamente

### Import de Dados
- Os dados de `stop_times.txt` são importados diretamente para o Cassandra
- Verifica duplicatas antes de inserir (por `trip_id` e `stop_sequence`)
- Usa batch statements para melhor performance

### Consultas
- **Por Trip ID**: Consulta otimizada usando partition key
- **Por Stop ID**: Usa índice secundário (ALLOW FILTERING quando necessário)
- **Cache**: Redis continua sendo usado para cache das consultas

## Vantagens da Migração

1. **Performance**: Melhor desempenho para leituras distribuídas
2. **Escalabilidade**: Fácil escalabilidade horizontal
3. **Modelo de Dados**: Partition key por `trip_id` otimiza consultas principais
4. **Volume de Dados**: Melhor para grandes volumes de dados de StopTimes

## Compatibilidade

A API permanece 100% compatível. Todas as rotas e métodos continuam funcionando da mesma forma:

- `GET /api/v1/tranzor/stop-times`
- `GET /api/v1/tranzor/stop-times/trip/{tripId}`
- `GET /api/v1/tranzor/stop-times/stop/{stopId}`

## Migração de Dados Existentes

Se você já possui dados de `stop_times` no PostgreSQL e deseja migrá-los:

1. Exporte os dados do PostgreSQL para CSV
2. Reimporte usando o método `ImportDataAsync` do serviço
3. Ou use ferramentas de migração como `sstableloader`

## Troubleshooting

### Erro de Conexão
Verifique se o Cassandra está rodando:
```bash
docker ps | grep cassandra
```

### Erro de Keyspace
O keyspace é criado automaticamente, mas você pode criá-lo manualmente:
```cql
CREATE KEYSPACE tranzor 
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};
```

### Performance
Para produção, considere:
- Aumentar `replication_factor` para 3
- Usar `NetworkTopologyStrategy` ao invés de `SimpleStrategy`
- Ajustar o batch size em `Constant.BatchSizeImport`
