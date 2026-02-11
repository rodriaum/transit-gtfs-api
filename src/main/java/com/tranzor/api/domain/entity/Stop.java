package com.tranzor.api.domain.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;
import org.locationtech.jts.geom.Point;

import java.time.LocalDateTime;

@Entity
@Table(name = "stops", indexes = {
    @Index(name = "idx_stop_id", columnList = "stop_id"),
    @Index(name = "idx_stop_code", columnList = "stop_code"),
    @Index(name = "idx_parent_station", columnList = "parent_station"),
    @Index(name = "idx_location_type", columnList = "location_type"),
    @Index(name = "idx_agency_id", columnList = "agency_id")
})
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Stop {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "stop_id", nullable = false, length = 100)
    private String stopId;

    @Column(name = "stop_code", length = 50)
    private String stopCode;

    @Column(name = "stop_name", nullable = false, length = 255)
    private String stopName;

    @Column(name = "stop_desc", columnDefinition = "TEXT")
    private String stopDesc;

    @Column(name = "stop_lat", nullable = false)
    private Double stopLat;

    @Column(name = "stop_lon", nullable = false)
    private Double stopLon;

    // PostGIS geography point for spatial queries
    @Column(name = "location", columnDefinition = "geography(Point, 4326)")
    private Point location;

    // PostGIS geometry point for faster queries
    @Column(name = "geom", columnDefinition = "geometry(Point, 4326)")
    private Point geom;

    @Column(name = "zone_id", length = 50)
    private String zoneId;

    @Column(name = "stop_url", length = 500)
    private String stopUrl;

    @Column(name = "location_type")
    private Integer locationType;

    @Column(name = "parent_station", length = 100)
    private String parentStation;

    @Column(name = "stop_timezone", length = 50)
    private String stopTimezone;

    @Column(name = "wheelchair_boarding")
    private Integer wheelchairBoarding;

    @Column(name = "level_id", length = 50)
    private String levelId;

    @Column(name = "platform_code", length = 50)
    private String platformCode;

    @Column(name = "agency_id", nullable = false, length = 100)
    private String agencyId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "agency_id", referencedColumnName = "agency_id", insertable = false, updatable = false)
    private Agency agency;

    @CreationTimestamp
    @Column(name = "created_at", updatable = false)
    private LocalDateTime createdAt;

    @UpdateTimestamp
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;

    @PrePersist
    @PreUpdate
    private void updateGeometry() {
        if (stopLat != null && stopLon != null && location == null) {
            // Geometry será criado por trigger no PostgreSQL
            // ou pode ser criado aqui se necessário
        }
    }
}
