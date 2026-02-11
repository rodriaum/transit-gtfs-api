package com.tranzor.api.domain.repository.cassandra;

import com.tranzor.api.domain.entity.cassandra.StopTimeByStop;
import org.springframework.data.cassandra.repository.CassandraRepository;
import org.springframework.data.cassandra.repository.Query;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;

/**
 * Cassandra repository for StopTimeByStop
 * 
 * Optimized for: "get next departures from stop X"
 * Partition key: stop_id + service_date
 * Clustering: departure_time (ordered)
 */
@Repository
public interface StopTimeByStopRepository extends CassandraRepository<StopTimeByStop, String> {

    /**
     * Find next departures from a stop on a specific date after a given time
     * This is THE key query for the "next departures" feature
     * 
     * Cassandra will efficiently scan only the relevant partition
     * and filter by departure_time
     */
    @Query("SELECT * FROM stop_times_by_stop " +
           "WHERE stop_id = ?0 AND service_date = ?1 AND departure_time >= ?2 " +
           "LIMIT ?3")
    List<StopTimeByStop> findNextDepartures(
        String stopId, 
        LocalDate serviceDate, 
        Integer departureTimeSeconds, 
        Integer limit
    );

    /**
     * Find next departures for a specific route
     */
    @Query("SELECT * FROM stop_times_by_stop " +
           "WHERE stop_id = ?0 AND service_date = ?1 AND departure_time >= ?2 " +
           "AND route_id = ?3 " +
           "LIMIT ?4 " +
           "ALLOW FILTERING")
    List<StopTimeByStop> findNextDeparturesByRoute(
        String stopId,
        LocalDate serviceDate,
        Integer departureTimeSeconds,
        String routeId,
        Integer limit
    );

    /**
     * Get all departures for a stop on a specific date
     * (for schedule display)
     */
    List<StopTimeByStop> findByStopIdAndServiceDate(String stopId, LocalDate serviceDate);

    /**
     * Get departures in a time range
     */
    @Query("SELECT * FROM stop_times_by_stop " +
           "WHERE stop_id = ?0 AND service_date = ?1 " +
           "AND departure_time >= ?2 AND departure_time <= ?3")
    List<StopTimeByStop> findDeparturesInTimeRange(
        String stopId,
        LocalDate serviceDate,
        Integer startTime,
        Integer endTime
    );

    /**
     * Delete all departures for a stop on a date (for reimport)
     */
    void deleteByStopIdAndServiceDate(String stopId, LocalDate serviceDate);

    /**
     * Delete by agency (for bulk reimport)
     */
    void deleteByAgencyId(String agencyId);
}
