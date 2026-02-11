package com.tranzor.api.domain.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;
import java.time.LocalTime;

@Entity
@Table(name = "stop_times", indexes = {
    @Index(name = "idx_trip_id_stoptime", columnList = "trip_id"),
    @Index(name = "idx_stop_id_stoptime", columnList = "stop_id"),
    @Index(name = "idx_stop_sequence", columnList = "stop_sequence"),
    @Index(name = "idx_arrival_time", columnList = "arrival_time"),
    @Index(name = "idx_departure_time", columnList = "departure_time")
})
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StopTime {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "trip_id", nullable = false, length = 100)
    private String tripId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "trip_id", referencedColumnName = "trip_id", insertable = false, updatable = false)
    private Trip trip;

    @Column(name = "arrival_time")
    private Integer arrivalTime; // Segundos desde meia-noite

    @Column(name = "departure_time")
    private Integer departureTime; // Segundos desde meia-noite

    @Column(name = "stop_id", nullable = false, length = 100)
    private String stopId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "stop_id", referencedColumnName = "stop_id", insertable = false, updatable = false)
    private Stop stop;

    @Column(name = "stop_sequence", nullable = false)
    private Integer stopSequence;

    @Column(name = "stop_headsign", length = 255)
    private String stopHeadsign;

    @Column(name = "pickup_type")
    private Integer pickupType;

    @Column(name = "drop_off_type")
    private Integer dropOffType;

    @Column(name = "continuous_pickup")
    private Integer continuousPickup;

    @Column(name = "continuous_drop_off")
    private Integer continuousDropOff;

    @Column(name = "shape_dist_traveled")
    private Double shapeDistTraveled;

    @Column(name = "timepoint")
    private Integer timepoint;

    @Column(name = "agency_id", nullable = false, length = 100)
    private String agencyId;

    @CreationTimestamp
    @Column(name = "created_at", updatable = false)
    private LocalDateTime createdAt;

    @UpdateTimestamp
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;

    // Método auxiliar para converter segundos em LocalTime
    public LocalTime getArrivalTimeAsLocalTime() {
        if (arrivalTime == null) return null;
        int hours = arrivalTime / 3600;
        int minutes = (arrivalTime % 3600) / 60;
        int seconds = arrivalTime % 60;
        
        // GTFS permite horas >= 24 para viagens que passam da meia-noite
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
