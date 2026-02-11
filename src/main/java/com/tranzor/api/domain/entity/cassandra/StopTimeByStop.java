package com.tranzor.api.domain.entity.cassandra;

import lombok.*;
import org.springframework.data.cassandra.core.cql.Ordering;
import org.springframework.data.cassandra.core.cql.PrimaryKeyType;
import org.springframework.data.cassandra.core.mapping.Column;
import org.springframework.data.cassandra.core.mapping.PrimaryKeyColumn;
import org.springframework.data.cassandra.core.mapping.Table;

import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalTime;

/**
 * StopTime by Stop - Materialized view for queries by stop_id
 * 
 * Design rationale:
 * - Partition key: stop_id + date (all departures for a stop on a specific day)
 * - Clustering keys: departure_time, trip_id (ordered by departure time)
 * - Optimized for: "get next departures from stop X"
 * 
 * This is essentially a materialized view of stop_times
 * optimized for different query patterns
 */
@Table("stop_times_by_stop")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StopTimeByStop {

    // Partition key - groups by stop and date
    @PrimaryKeyColumn(name = "stop_id", ordinal = 0, type = PrimaryKeyType.PARTITIONED)
    private String stopId;

    @PrimaryKeyColumn(name = "service_date", ordinal = 1, type = PrimaryKeyType.PARTITIONED)
    private LocalDate serviceDate;

    // Clustering columns - ordered by departure time
    @PrimaryKeyColumn(name = "departure_time", ordinal = 2, type = PrimaryKeyType.CLUSTERED, ordering = Ordering.ASCENDING)
    private Integer departureTime;

    @PrimaryKeyColumn(name = "trip_id", ordinal = 3, type = PrimaryKeyType.CLUSTERED, ordering = Ordering.ASCENDING)
    private String tripId;

    @Column("arrival_time")
    private Integer arrivalTime;

    @Column("stop_sequence")
    private Integer stopSequence;

    @Column("stop_headsign")
    private String stopHeadsign;

    @Column("pickup_type")
    private Integer pickupType;

    @Column("drop_off_type")
    private Integer dropOffType;

    @Column("route_id")
    private String routeId;

    @Column("route_short_name")
    private String routeShortName;

    @Column("route_long_name")
    private String routeLongName;

    @Column("route_color")
    private String routeColor;

    @Column("route_text_color")
    private String routeTextColor;

    @Column("route_type")
    private Integer routeType;

    @Column("trip_headsign")
    private String tripHeadsign;

    @Column("direction_id")
    private Integer directionId;

    @Column("service_id")
    private String serviceId;

    @Column("agency_id")
    private String agencyId;

    @Column("wheelchair_accessible")
    private Integer wheelchairAccessible;

    @Column("bikes_allowed")
    private Integer bikesAllowed;

    @Column("created_at")
    private Instant createdAt;

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
