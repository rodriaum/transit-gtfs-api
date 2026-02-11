package com.tranzor.api.domain.entity.cassandra;

import lombok.*;
import org.springframework.data.cassandra.core.cql.Ordering;
import org.springframework.data.cassandra.core.cql.PrimaryKeyType;
import org.springframework.data.cassandra.core.mapping.Column;
import org.springframework.data.cassandra.core.mapping.PrimaryKeyColumn;
import org.springframework.data.cassandra.core.mapping.Table;

import java.time.Instant;
import java.time.LocalTime;

/**
 * StopTime entity for Cassandra
 * 
 * Design rationale:
 * - Partition key: trip_id (all stop_times for a trip together)
 * - Clustering key: stop_sequence (ordered within partition)
 * - Optimized for queries like "get all stops for trip X"
 * 
 * This table handles MASSIVE volume (millions of records)
 * while PostgreSQL handles relational metadata (stops, routes, trips)
 */
@Table("stop_times")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StopTimeCassandra {

    // Partition key - groups all stop_times for a trip together
    @PrimaryKeyColumn(name = "trip_id", ordinal = 0, type = PrimaryKeyType.PARTITIONED)
    private String tripId;

    // Clustering column - orders stops within a trip
    @PrimaryKeyColumn(name = "stop_sequence", ordinal = 1, type = PrimaryKeyType.CLUSTERED, ordering = Ordering.ASCENDING)
    private Integer stopSequence;

    @Column("stop_id")
    private String stopId;

    @Column("arrival_time")
    private Integer arrivalTime; // Segundos desde meia-noite

    @Column("departure_time")
    private Integer departureTime; // Segundos desde meia-noite

    @Column("stop_headsign")
    private String stopHeadsign;

    @Column("pickup_type")
    private Integer pickupType;

    @Column("drop_off_type")
    private Integer dropOffType;


    @Column("shape_dist_traveled")
    private Double shapeDistTraveled;

    @Column("timepoint")
    private Integer timepoint;

    @Column("agency_id")
    private String agencyId;

    @Column("route_id")
    private String routeId; // Denormalized for faster queries

    @Column("service_id")
    private String serviceId; // Denormalized for faster queries

    @Column("created_at")
    private Instant createdAt;

    @Column("updated_at")
    private Instant updatedAt;

    // Helper methods
    public LocalTime getArrivalTimeAsLocalTime() {
        if (arrivalTime == null) return null;
        int hours = arrivalTime / 3600;
        int minutes = (arrivalTime % 3600) / 60;
        int seconds = arrivalTime % 60;
        
        if (hours >= 24) {
            hours = hours % 24;
        }
        
        return LocalTime.of(hours, minutes, seconds);
    }

    public LocalTime getDepartureTimeAsLocalTime() {
        if (departureTime == null) return null;
        int hours = departureTime / 3600;
        int minutes = (departureTime % 3600) / 60;
        int seconds = departureTime % 60;
        
        if (hours >= 24) {
            hours = hours % 24;
        }
        
        return LocalTime.of(hours, minutes, seconds);
    }
}
