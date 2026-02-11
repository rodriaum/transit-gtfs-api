package com.tranzor.api.domain.repository;

import com.tranzor.api.domain.entity.Trip;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

@Repository
public interface TripRepository extends JpaRepository<Trip, Long> {

    Optional<Trip> findByTripIdAndAgencyId(String tripId, String agencyId);

    List<Trip> findByRouteId(String routeId);

    List<Trip> findByAgencyId(String agencyId);

    Page<Trip> findByRouteIdAndAgencyId(String routeId, String agencyId, Pageable pageable);

    // Viagens ativas para um serviço e data
    @Query(value = """
        SELECT t.*
        FROM trips t
        INNER JOIN calendar c ON t.service_id = c.service_id
        WHERE t.agency_id = :agencyId
        AND c.start_date <= :date
        AND c.end_date >= :date
        AND CASE EXTRACT(DOW FROM :date::date)
            WHEN 0 THEN c.sunday
            WHEN 1 THEN c.monday
            WHEN 2 THEN c.tuesday
            WHEN 3 THEN c.wednesday
            WHEN 4 THEN c.thursday
            WHEN 5 THEN c.friday
            WHEN 6 THEN c.saturday
        END = true
        AND NOT EXISTS (
            SELECT 1 FROM calendar_dates cd
            WHERE cd.service_id = t.service_id
            AND cd.date = :date
            AND cd.exception_type = 2
        )
        """, nativeQuery = true)
    List<Trip> findActiveTripsForDate(
        @Param("agencyId") String agencyId,
        @Param("date") LocalDate date
    );

    // Viagens por rota em uma data específica
    @Query(value = """
        SELECT t.*
        FROM trips t
        INNER JOIN calendar c ON t.service_id = c.service_id
        WHERE t.route_id = :routeId
        AND t.agency_id = :agencyId
        AND c.start_date <= :date
        AND c.end_date >= :date
        AND CASE EXTRACT(DOW FROM :date::date)
            WHEN 0 THEN c.sunday
            WHEN 1 THEN c.monday
            WHEN 2 THEN c.tuesday
            WHEN 3 THEN c.wednesday
            WHEN 4 THEN c.thursday
            WHEN 5 THEN c.friday
            WHEN 6 THEN c.saturday
        END = true
        """, nativeQuery = true)
    List<Trip> findTripsByRouteAndDate(
        @Param("routeId") String routeId,
        @Param("agencyId") String agencyId,
        @Param("date") LocalDate date
    );

    long countByAgencyId(String agencyId);
}
