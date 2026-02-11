package com.tranzor.api.domain.repository;

import com.tranzor.api.domain.entity.StopTime;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;

@Repository
public interface StopTimeRepository extends JpaRepository<StopTime, Long> {

    List<StopTime> findByStopIdOrderByDepartureTime(String stopId);

    List<StopTime> findByTripIdOrderByStopSequence(String tripId);

    // Próximas partidas para uma paragem específica
    @Query(value = """
        SELECT DISTINCT ON (st.trip_id) st.*
        FROM stop_times st
        INNER JOIN trips t ON st.trip_id = t.trip_id
        INNER JOIN calendar c ON t.service_id = c.service_id
        WHERE st.stop_id = :stopId
        AND st.agency_id = :agencyId
        AND st.departure_time >= :currentTimeSeconds
        AND c.start_date <= :currentDate
        AND c.end_date >= :currentDate
        AND CASE EXTRACT(DOW FROM :currentDate::date)
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
            AND cd.date = :currentDate
            AND cd.exception_type = 2
        )
        ORDER BY st.trip_id, st.departure_time
        LIMIT :limit
        """, nativeQuery = true)
    List<StopTime> findNextDepartures(
        @Param("stopId") String stopId,
        @Param("agencyId") String agencyId,
        @Param("currentTimeSeconds") Integer currentTimeSeconds,
        @Param("currentDate") LocalDate currentDate,
        @Param("limit") Integer limit
    );

    // Próximas partidas com filtro de rota
    @Query(value = """
        SELECT st.*
        FROM stop_times st
        INNER JOIN trips t ON st.trip_id = t.trip_id
        INNER JOIN calendar c ON t.service_id = c.service_id
        WHERE st.stop_id = :stopId
        AND t.route_id = :routeId
        AND st.agency_id = :agencyId
        AND st.departure_time >= :currentTimeSeconds
        AND c.start_date <= :currentDate
        AND c.end_date >= :currentDate
        AND CASE EXTRACT(DOW FROM :currentDate::date)
            WHEN 0 THEN c.sunday
            WHEN 1 THEN c.monday
            WHEN 2 THEN c.tuesday
            WHEN 3 THEN c.wednesday
            WHEN 4 THEN c.thursday
            WHEN 5 THEN c.friday
            WHEN 6 THEN c.saturday
        END = true
        ORDER BY st.departure_time
        LIMIT :limit
        """, nativeQuery = true)
    List<StopTime> findNextDeparturesByRoute(
        @Param("stopId") String stopId,
        @Param("routeId") String routeId,
        @Param("agencyId") String agencyId,
        @Param("currentTimeSeconds") Integer currentTimeSeconds,
        @Param("currentDate") LocalDate currentDate,
        @Param("limit") Integer limit
    );

    // Horários completos de uma paragem para um dia específico
    @Query(value = """
        SELECT st.*
        FROM stop_times st
        INNER JOIN trips t ON st.trip_id = t.trip_id
        INNER JOIN calendar c ON t.service_id = c.service_id
        WHERE st.stop_id = :stopId
        AND st.agency_id = :agencyId
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
        ORDER BY st.departure_time
        """, nativeQuery = true)
    List<StopTime> findScheduleForDate(
        @Param("stopId") String stopId,
        @Param("agencyId") String agencyId,
        @Param("date") LocalDate date
    );

    long countByAgencyId(String agencyId);
}
