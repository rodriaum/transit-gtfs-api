package com.tranzor.api.domain.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "routes", indexes = {
    @Index(name = "idx_route_id", columnList = "route_id"),
    @Index(name = "idx_agency_id_route", columnList = "agency_id"),
    @Index(name = "idx_route_type", columnList = "route_type")
})
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Route {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "route_id", nullable = false, length = 100)
    private String routeId;

    @Column(name = "agency_id", nullable = false, length = 100)
    private String agencyId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "agency_id", referencedColumnName = "agency_id", insertable = false, updatable = false)
    private Agency agency;

    @Column(name = "route_short_name", length = 50)
    private String routeShortName;

    @Column(name = "route_long_name", length = 255)
    private String routeLongName;

    @Column(name = "route_desc", columnDefinition = "TEXT")
    private String routeDesc;

    @Column(name = "route_type", nullable = false)
    private Integer routeType;

    @Column(name = "route_url", length = 500)
    private String routeUrl;

    @Column(name = "route_color", length = 6)
    private String routeColor;

    @Column(name = "route_text_color", length = 6)
    private String routeTextColor;

    @Column(name = "route_sort_order")
    private Integer routeSortOrder;

    @Column(name = "continuous_pickup")
    private Integer continuousPickup;

    @Column(name = "continuous_drop_off")
    private Integer continuousDropOff;

    @CreationTimestamp
    @Column(name = "created_at", updatable = false)
    private LocalDateTime createdAt;

    @UpdateTimestamp
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;
}
