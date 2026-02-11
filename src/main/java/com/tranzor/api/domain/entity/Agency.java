package com.tranzor.api.domain.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.annotations.UpdateTimestamp;
import org.hibernate.type.SqlTypes;

import java.time.LocalDateTime;

@Entity
@Table(name = "agencies", indexes = {
    @Index(name = "idx_agency_id", columnList = "agency_id")
})
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Agency {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "agency_id", unique = true, nullable = false, length = 100)
    private String agencyId;

    @Column(name = "name", nullable = false, length = 255)
    private String name;

    @Column(name = "url", length = 500)
    private String url;

    @Column(name = "timezone", nullable = false, length = 50)
    private String timezone;

    @Column(name = "lang", length = 10)
    private String lang;

    @Column(name = "phone", length = 50)
    private String phone;

    @Column(name = "fare_url", length = 500)
    private String fareUrl;

    @Column(name = "email", length = 255)
    private String email;

    @Column(name = "image_url", length = 500)
    private String imageUrl;

    @Column(name = "gtfs_url", length = 500)
    private String gtfsUrl;

    @Column(name = "gtfs_expire_at")
    private LocalDateTime gtfsExpireAt;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(name = "realtime_urls", columnDefinition = "jsonb")
    private String realtimeUrls;

    @Column(name = "active")
    private Boolean active = true;

    @CreationTimestamp
    @Column(name = "created_at", updatable = false)
    private LocalDateTime createdAt;

    @UpdateTimestamp
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;

    @Column(name = "last_gtfs_import")
    private LocalDateTime lastGtfsImport;
}
