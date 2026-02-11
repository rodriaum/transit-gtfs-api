package com.tranzor.api.domain.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDate;
import java.time.LocalDateTime;

@Entity
@Table(name = "calendar_dates", indexes = {
    @Index(name = "idx_service_id_date", columnList = "service_id"),
    @Index(name = "idx_date", columnList = "date"),
    @Index(name = "idx_exception_type", columnList = "exception_type")
})
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class CalendarDate {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "service_id", nullable = false, length = 100)
    private String serviceId;

    @Column(name = "date", nullable = false)
    private LocalDate date;

    @Column(name = "exception_type", nullable = false)
    private Integer exceptionType; // 1 = added, 2 = removed

    @Column(name = "agency_id", nullable = false, length = 100)
    private String agencyId;

    @CreationTimestamp
    @Column(name = "created_at", updatable = false)
    private LocalDateTime createdAt;

    @UpdateTimestamp
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;
}
