package com.tranzor.api.domain.repository.cassandra;

import com.tranzor.api.domain.entity.cassandra.StopTimeCassandra;
import org.springframework.data.cassandra.repository.CassandraRepository;
import org.springframework.data.cassandra.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * Cassandra repository for StopTimes
 * 
 * Optimized for high-volume time-series data
 * Primary query pattern: get all stops for a trip
 */
@Repository
public interface StopTimeCassandraRepository extends CassandraRepository<StopTimeCassandra, String> {

    /**
     * Find all stop times for a specific trip, ordered by stop sequence
     * This is the primary query pattern - very efficient in Cassandra
     */
    List<StopTimeCassandra> findByTripIdOrderByStopSequence(String tripId);

    /**
     * Find all stop times for a trip
     */
    List<StopTimeCassandra> findByTripId(String tripId);

    /**
     * Find stop times for multiple trips (for batch operations)
     */
    @Query("SELECT * FROM stop_times WHERE trip_id IN ?0")
    List<StopTimeCassandra> findByTripIdIn(List<String> tripIds);

    /**
     * Find stop times by agency (for bulk operations)
     */
    List<StopTimeCassandra> findByAgencyId(String agencyId);

    /**
     * Delete all stop times for a trip
     */
    void deleteByTripId(String tripId);

    /**
     * Delete all stop times for an agency (for reimport)
     */
    void deleteByAgencyId(String agencyId);
}
