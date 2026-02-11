-- Cassandra Schema for Tranzor API
-- High-volume time-series data optimized for write and read performance

-- Create keyspace
CREATE KEYSPACE IF NOT EXISTS tranzor
WITH REPLICATION = {
    'class': 'SimpleStrategy',
    'replication_factor': 3
};

USE tranzor;

-- ================================================================
-- Table 1: stop_times (partitioned by trip_id)
-- ================================================================
-- Query pattern: "Get all stops for trip X in order"
-- Partition key: trip_id (all stops for a trip together)
-- Clustering key: stop_sequence (ordered)
-- ================================================================

CREATE TABLE IF NOT EXISTS stop_times (
    trip_id text,
    stop_sequence int,
    stop_id text,
    arrival_time int,          -- seconds since midnight
    departure_time int,        -- seconds since midnight
    stop_headsign text,
    pickup_type int,
    drop_off_type int,
    continuous_pickup int,
    continuous_drop_off int,
    shape_dist_traveled double,
    timepoint int,
    agency_id text,
    route_id text,             -- denormalized
    service_id text,           -- denormalized
    created_at timestamp,
    updated_at timestamp,
    PRIMARY KEY (trip_id, stop_sequence)
) WITH CLUSTERING ORDER BY (stop_sequence ASC)
  AND comment = 'Stop times partitioned by trip - optimized for trip schedule queries';

-- Index for querying by agency (for bulk operations)
CREATE INDEX IF NOT EXISTS idx_stop_times_agency 
ON stop_times (agency_id);


-- ================================================================
-- Table 2: stop_times_by_stop (partitioned by stop_id + date)
-- ================================================================
-- Query pattern: "Get next departures from stop X on date Y after time Z"
-- Partition key: stop_id + service_date
-- Clustering keys: departure_time, trip_id (ordered by time)
-- This is essentially a materialized view optimized for departure queries
-- ================================================================

CREATE TABLE IF NOT EXISTS stop_times_by_stop (
    stop_id text,
    service_date date,
    departure_time int,
    trip_id text,
    arrival_time int,
    stop_sequence int,
    stop_headsign text,
    pickup_type int,
    drop_off_type int,
    -- Denormalized route info for fast queries
    route_id text,
    route_short_name text,
    route_long_name text,
    route_color text,
    route_text_color text,
    route_type int,
    -- Denormalized trip info
    trip_headsign text,
    direction_id int,
    service_id text,
    agency_id text,
    wheelchair_accessible int,
    bikes_allowed int,
    created_at timestamp,
    PRIMARY KEY ((stop_id, service_date), departure_time, trip_id)
) WITH CLUSTERING ORDER BY (departure_time ASC, trip_id ASC)
  AND comment = 'Stop times partitioned by stop and date - optimized for next departures queries';

-- Index for querying by agency (for bulk operations)
CREATE INDEX IF NOT EXISTS idx_stop_times_by_stop_agency 
ON stop_times_by_stop (agency_id);

-- Index for querying by route (optional - can use ALLOW FILTERING)
CREATE INDEX IF NOT EXISTS idx_stop_times_by_stop_route 
ON stop_times_by_stop (route_id);


-- ================================================================
-- Table 3: stop_times_history (for analytics and historical data)
-- ================================================================
-- Query pattern: Historical analysis, delay tracking
-- Partition key: stop_id + year_month
-- Clustering key: timestamp
-- Optional: Only if you need historical tracking
-- ================================================================

CREATE TABLE IF NOT EXISTS stop_times_history (
    stop_id text,
    year_month text,           -- format: YYYY-MM
    timestamp timestamp,
    trip_id text,
    route_id text,
    scheduled_departure_time int,
    actual_departure_time int,
    delay_seconds int,
    vehicle_id text,
    occupancy_status text,
    created_at timestamp,
    PRIMARY KEY ((stop_id, year_month), timestamp, trip_id)
) WITH CLUSTERING ORDER BY (timestamp DESC, trip_id ASC)
  AND comment = 'Historical stop times for analytics and delay tracking'
  AND default_time_to_live = 7776000;  -- 90 days retention


-- ================================================================
-- Table 4: realtime_positions (for vehicle tracking)
-- ================================================================
-- Query pattern: Get current position of vehicles
-- Partition key: vehicle_id
-- Clustering key: timestamp
-- TTL: 24 hours (realtime data doesn't need long retention)
-- ================================================================

CREATE TABLE IF NOT EXISTS realtime_positions (
    vehicle_id text,
    timestamp timestamp,
    trip_id text,
    route_id text,
    latitude double,
    longitude double,
    bearing float,
    speed float,
    occupancy_status text,
    stop_id text,             -- current or next stop
    stop_sequence int,
    current_status text,      -- IN_TRANSIT_TO, STOPPED_AT, etc
    agency_id text,
    created_at timestamp,
    PRIMARY KEY (vehicle_id, timestamp)
) WITH CLUSTERING ORDER BY (timestamp DESC)
  AND default_time_to_live = 86400  -- 24 hour TTL
  AND comment = 'Realtime vehicle positions with 24h retention';

-- Index for querying by route
CREATE INDEX IF NOT EXISTS idx_realtime_positions_route 
ON realtime_positions (route_id);


-- ================================================================
-- Materialized Views (if needed for different query patterns)
-- ================================================================

-- Alternative: Use materialized view instead of maintaining two tables
-- Note: Materialized views have performance implications in Cassandra
-- Only use if write volume is manageable

-- CREATE MATERIALIZED VIEW stop_times_by_stop_mv AS
--     SELECT * FROM stop_times
--     WHERE stop_id IS NOT NULL 
--       AND trip_id IS NOT NULL 
--       AND stop_sequence IS NOT NULL
--       AND departure_time IS NOT NULL
--     PRIMARY KEY ((stop_id, service_date), departure_time, trip_id)
--     WITH CLUSTERING ORDER BY (departure_time ASC);


-- ================================================================
-- Performance Tips
-- ================================================================

-- 1. Always query with partition key (stop_id, trip_id, etc)
-- 2. Use clustering columns for ordering (no ORDER BY needed)
-- 3. Avoid ALLOW FILTERING in production (use indexes)
-- 4. Batch writes for imports (max 65KB per batch)
-- 5. Use prepared statements for better performance
-- 6. Monitor partition sizes (avoid hotspots)
-- 7. Consider bucket strategy for very high-volume stops
-- 8. Use TTL for realtime/historical data

-- Example partition bucketing for high-volume stops:
-- Instead of: PRIMARY KEY (stop_id, ...)
-- Use: PRIMARY KEY ((stop_id, bucket), ...)
-- Where bucket = hour_of_day or date_partition


-- ================================================================
-- Sample Queries
-- ================================================================

-- Get all stops for a trip (very fast - single partition)
-- SELECT * FROM stop_times WHERE trip_id = 'TRIP_123';

-- Get next 10 departures from a stop (very fast - single partition + range)
-- SELECT * FROM stop_times_by_stop 
-- WHERE stop_id = 'STOP_456' AND service_date = '2026-02-11' 
--   AND departure_time >= 32400 
-- LIMIT 10;

-- Get departures for a route at a stop (uses index)
-- SELECT * FROM stop_times_by_stop 
-- WHERE stop_id = 'STOP_456' AND service_date = '2026-02-11' 
--   AND departure_time >= 32400 
--   AND route_id = 'ROUTE_789'
-- LIMIT 10;

-- Get current vehicle positions for a route
-- SELECT * FROM realtime_positions 
-- WHERE route_id = 'ROUTE_789' 
--   AND timestamp > toTimestamp(now()) - 5m;
