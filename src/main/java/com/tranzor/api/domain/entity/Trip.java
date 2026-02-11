package com.tranzor.api.domain.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "trips", indexes = {
    @Index(name = "idx_trip_id", columnList = "trip_id"),
    @Index(name = "idx_route_id_trip", columnList = "route_id"),
    @Index(name = "idx_service_id", columnList = "service_id"),
    @Index(name = "idx_direction_id", columnList = "direction_id")
})
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Trip {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "trip_id", nullable = false, length = 100)
    private String tripId;

    @Column(name = "route_id", nullable = false, length = 100)
    private String routeId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "route_id", referencedColumnName = "route_id", insertable = false, updatable = false)
    private Route route;

    @Column(name = "service_id", nullable = false, length = 100)
    private String serviceId;

    @Column(name = "trip_headsign", length = 255)
    private String tripHeadsign;

    @Column(name = "trip_short_name", length = 50)
    private String tripShortName;

    @Column(name = "direction_id")
    private Integer directionId;

    @Column(name = "block_id", length = 50)
    private String blockId;

    @Column(name = "shape_id", length = 100)
    private String shapeId;

    @Column(name = "wheelchair_accessible")
    private Integer wheelchairAccessible;

    @Column(name = "bikes_allowed")
    private Integer bikesAllowed;

    @Column(name = "agency_id", nullable = false, length = 100)
    private String agencyId;

    @CreationTimestamp
    @Column(name = "created_at", updatable = false)
    private LocalDateTime createdAt;

    @UpdateTimestamp
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;
}
