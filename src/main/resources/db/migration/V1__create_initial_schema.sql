-- V1__create_initial_schema.sql

-- Enable PostGIS extension
CREATE EXTENSION IF NOT EXISTS postgis;

-- Agency table
CREATE TABLE agencies (
    id BIGSERIAL PRIMARY KEY,
    agency_id VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    url VARCHAR(500),
    timezone VARCHAR(50) NOT NULL,
    lang VARCHAR(10),
    phone VARCHAR(50),
    fare_url VARCHAR(500),
    email VARCHAR(255),
    image_url VARCHAR(500),
    gtfs_url VARCHAR(500),
    gtfs_expire_at TIMESTAMP,
    realtime_urls JSONB,
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_gtfs_import TIMESTAMP
);

CREATE INDEX idx_agency_id ON agencies(agency_id);
CREATE INDEX idx_agency_active ON agencies(active);

-- Stops table with PostGIS
CREATE TABLE stops (
    id BIGSERIAL PRIMARY KEY,
    stop_id VARCHAR(100) NOT NULL,
    stop_code VARCHAR(50),
    stop_name VARCHAR(255) NOT NULL,
    stop_desc TEXT,
    stop_lat DOUBLE PRECISION NOT NULL,
    stop_lon DOUBLE PRECISION NOT NULL,
    location geography(Point, 4326),
    geom geometry(Point, 4326),
    zone_id VARCHAR(50),
    stop_url VARCHAR(500),
    location_type INTEGER,
    parent_station VARCHAR(100),
    stop_timezone VARCHAR(50),
    wheelchair_boarding INTEGER,
    level_id VARCHAR(50),
    platform_code VARCHAR(50),
    agency_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (agency_id) REFERENCES agencies(agency_id)
);

CREATE INDEX idx_stop_id ON stops(stop_id);
CREATE INDEX idx_stop_code ON stops(stop_code);
CREATE INDEX idx_stop_parent_station ON stops(parent_station);
CREATE INDEX idx_stop_location_type ON stops(location_type);
CREATE INDEX idx_stop_agency_id ON stops(agency_id);
CREATE INDEX idx_stop_location ON stops USING GIST(location);
CREATE INDEX idx_stop_geom ON stops USING GIST(geom);

-- Trigger to auto-populate geometry from lat/lon
CREATE OR REPLACE FUNCTION update_stop_geometry()
RETURNS TRIGGER AS $$
BEGIN
    NEW.location = ST_SetSRID(ST_MakePoint(NEW.stop_lon, NEW.stop_lat), 4326)::geography;
    NEW.geom = ST_SetSRID(ST_MakePoint(NEW.stop_lon, NEW.stop_lat), 4326);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_stop_geometry
BEFORE INSERT OR UPDATE ON stops
FOR EACH ROW
EXECUTE FUNCTION update_stop_geometry();

-- Routes table
CREATE TABLE routes (
    id BIGSERIAL PRIMARY KEY,
    route_id VARCHAR(100) NOT NULL,
    agency_id VARCHAR(100) NOT NULL,
    route_short_name VARCHAR(50),
    route_long_name VARCHAR(255),
    route_desc TEXT,
    route_type INTEGER NOT NULL,
    route_url VARCHAR(500),
    route_color VARCHAR(6),
    route_text_color VARCHAR(6),
    route_sort_order INTEGER,
    continuous_pickup INTEGER,
    continuous_drop_off INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (agency_id) REFERENCES agencies(agency_id)
);

CREATE INDEX idx_route_id ON routes(route_id);
CREATE INDEX idx_route_agency_id ON routes(agency_id);
CREATE INDEX idx_route_type ON routes(route_type);

-- Trips table
CREATE TABLE trips (
    id BIGSERIAL PRIMARY KEY,
    trip_id VARCHAR(100) NOT NULL,
    route_id VARCHAR(100) NOT NULL,
    service_id VARCHAR(100) NOT NULL,
    trip_headsign VARCHAR(255),
    trip_short_name VARCHAR(50),
    direction_id INTEGER,
    block_id VARCHAR(50),
    shape_id VARCHAR(100),
    wheelchair_accessible INTEGER,
    bikes_allowed INTEGER,
    agency_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_trip_id ON trips(trip_id);
CREATE INDEX idx_trip_route_id ON trips(route_id);
CREATE INDEX idx_trip_service_id ON trips(service_id);
CREATE INDEX idx_trip_direction_id ON trips(direction_id);

-- Calendar table
CREATE TABLE calendar (
    id BIGSERIAL PRIMARY KEY,
    service_id VARCHAR(100) NOT NULL,
    monday BOOLEAN NOT NULL,
    tuesday BOOLEAN NOT NULL,
    wednesday BOOLEAN NOT NULL,
    thursday BOOLEAN NOT NULL,
    friday BOOLEAN NOT NULL,
    saturday BOOLEAN NOT NULL,
    sunday BOOLEAN NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    agency_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_calendar_service_id ON calendar(service_id);
CREATE INDEX idx_calendar_start_date ON calendar(start_date);
CREATE INDEX idx_calendar_end_date ON calendar(end_date);

-- Calendar dates table
CREATE TABLE calendar_dates (
    id BIGSERIAL PRIMARY KEY,
    service_id VARCHAR(100) NOT NULL,
    date DATE NOT NULL,
    exception_type INTEGER NOT NULL,
    agency_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_calendardate_service_id ON calendar_dates(service_id);
CREATE INDEX idx_calendardate_date ON calendar_dates(date);
CREATE INDEX idx_calendardate_exception ON calendar_dates(exception_type);

-- Comments
COMMENT ON TABLE agencies IS 'Agências de transporte público';
COMMENT ON TABLE stops IS 'Paragens/Estações de transporte';
COMMENT ON TABLE routes IS 'Rotas/Linhas de transporte';
COMMENT ON TABLE trips IS 'Viagens/Percursos';
COMMENT ON TABLE calendar IS 'Calendários de serviço';
COMMENT ON TABLE calendar_dates IS 'Exceções de calendário';
