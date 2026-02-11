package com.tranzor.api.service;

import com.tranzor.api.domain.entity.Agency;
import com.tranzor.api.domain.entity.Route;
import com.tranzor.api.domain.entity.Stop;
import com.tranzor.api.domain.entity.Trip;
import com.tranzor.api.domain.entity.cassandra.StopTimeByStop;
import com.tranzor.api.domain.entity.cassandra.StopTimeCassandra;
import com.tranzor.api.domain.repository.AgencyRepository;
import com.tranzor.api.domain.repository.RouteRepository;
import com.tranzor.api.domain.repository.StopRepository;
import com.tranzor.api.domain.repository.TripRepository;
import com.tranzor.api.domain.repository.cassandra.StopTimeByStopRepository;
import com.tranzor.api.domain.repository.cassandra.StopTimeCassandraRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.locationtech.jts.geom.Coordinate;
import org.locationtech.jts.geom.GeometryFactory;
import org.locationtech.jts.geom.Point;
import org.locationtech.jts.geom.PrecisionModel;
import org.onebusaway.gtfs.impl.GtfsRelationalDaoImpl;
import org.onebusaway.gtfs.serialization.GtfsReader;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.io.File;
import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * GTFS Import Service - Hybrid Architecture
 * <p>
 * PostgreSQL: agencies, stops, routes, trips, calendar
 * Cassandra: stop_times (high volume, time-series)
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class GTFSImportService {

    // PostgreSQL repositories
    private final AgencyRepository agencyRepository;
    private final StopRepository stopRepository;
    private final RouteRepository routeRepository;
    private final TripRepository tripRepository;

    // Cassandra repositories
    private final StopTimeCassandraRepository stopTimeCassandraRepository;
    private final StopTimeByStopRepository stopTimeByStopRepository;

    private final GeometryFactory geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

    @Transactional
    public void importGTFS(String agencyId, File gtfsZipFile) {
        log.info("Starting GTFS import for agency: {}", agencyId);

        try {
            GtfsReader reader = new GtfsReader();
            reader.setInputLocation(gtfsZipFile);

            GtfsRelationalDaoImpl store = new GtfsRelationalDaoImpl();
            reader.setEntityStore(store);
            reader.run();

            // Import to PostgreSQL (relational metadata)
            importAgencies(store, agencyId);
            importStops(store, agencyId);
            importRoutes(store, agencyId);
            importTrips(store, agencyId);
            importCalendar(store, agencyId);
            importCalendarDates(store, agencyId);

            // Import to Cassandra (high-volume time-series)
            importStopTimesToCassandra(store, agencyId);

            // Update import timestamp
            updateAgencyImportTimestamp(agencyId);

            log.info("GTFS import completed successfully for agency: {}", agencyId);

        } catch (Exception e) {
            log.error("Error importing GTFS for agency: {}", agencyId, e);
            throw new RuntimeException("Failed to import GTFS data", e);
        }
    }

    private void importAgencies(GtfsRelationalDaoImpl store, String mainAgencyId) {
        log.info("Importing agencies to PostgreSQL...");

        for (org.onebusaway.gtfs.model.Agency gtfsAgency : store.getAllAgencies()) {
            Agency agency = Agency.builder()
                    .agencyId(gtfsAgency.getId())
                    .name(gtfsAgency.getName())
                    .url(gtfsAgency.getUrl())
                    .timezone(gtfsAgency.getTimezone())
                    .lang(gtfsAgency.getLang())
                    .phone(gtfsAgency.getPhone())
                    .fareUrl(gtfsAgency.getFareUrl())
                    .active(true)
                    .build();

            agencyRepository.save(agency);
        }

        log.info("Imported {} agencies to PostgreSQL", store.getAllAgencies().size());
    }

    private void importStops(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing stops to PostgreSQL...");

        List<Stop> stops = new ArrayList<>();

        for (org.onebusaway.gtfs.model.Stop gtfsStop : store.getAllStops()) {
            Point location = geometryFactory.createPoint(
                    new Coordinate(gtfsStop.getLon(), gtfsStop.getLat())
            );

            Stop stop = Stop.builder()
                    .stopId(gtfsStop.getId().getId())
                    .stopCode(gtfsStop.getCode())
                    .stopName(gtfsStop.getName())
                    .stopDesc(gtfsStop.getDesc())
                    .stopLat(gtfsStop.getLat())
                    .stopLon(gtfsStop.getLon())
                    .location(location)
                    .geom(location)
                    .zoneId(gtfsStop.getZoneId())
                    .stopUrl(gtfsStop.getUrl())
                    .locationType(gtfsStop.getLocationType())
                    .parentStation(gtfsStop.getParentStation())
                    .stopTimezone(gtfsStop.getTimezone())
                    .wheelchairBoarding(gtfsStop.getWheelchairBoarding())
                    .agencyId(agencyId)
                    .build();

            stops.add(stop);

            // Batch save for performance
            if (stops.size() >= 1000) {
                stopRepository.saveAll(stops);
                stops.clear();
            }
        }

        if (!stops.isEmpty()) {
            stopRepository.saveAll(stops);
        }

        log.info("Imported {} stops to PostgreSQL", store.getAllStops().size());
    }

    private void importRoutes(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing routes to PostgreSQL...");

        for (org.onebusaway.gtfs.model.Route gtfsRoute : store.getAllRoutes()) {
            Route route = Route.builder()
                    .routeId(gtfsRoute.getId().getId())
                    .agencyId(agencyId)
                    .routeShortName(gtfsRoute.getShortName())
                    .routeLongName(gtfsRoute.getLongName())
                    .routeDesc(gtfsRoute.getDesc())
                    .routeType(gtfsRoute.getType())
                    .routeUrl(gtfsRoute.getUrl())
                    .routeColor(gtfsRoute.getColor())
                    .routeTextColor(gtfsRoute.getTextColor())
                    .routeSortOrder(gtfsRoute.getSortOrder())
                    .build();

            routeRepository.save(route);
        }

        log.info("Imported {} routes to PostgreSQL", store.getAllRoutes().size());
    }

    private void importTrips(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing trips to PostgreSQL...");

        List<Trip> trips = new ArrayList<>();

        for (org.onebusaway.gtfs.model.Trip gtfsTrip : store.getAllTrips()) {
            Trip trip = Trip.builder()
                    .tripId(gtfsTrip.getId().getId())
                    .routeId(gtfsTrip.getRoute().getId().getId())
                    .serviceId(gtfsTrip.getServiceId().getId())
                    .tripHeadsign(gtfsTrip.getTripHeadsign())
                    .tripShortName(gtfsTrip.getTripShortName())
                    .directionId(gtfsTrip.getDirectionId() != null ?
                            Integer.parseInt(gtfsTrip.getDirectionId()) : null)
                    .blockId(gtfsTrip.getBlockId())
                    .shapeId(gtfsTrip.getShapeId() != null ?
                            gtfsTrip.getShapeId().getId() : null)
                    .wheelchairAccessible(gtfsTrip.getWheelchairAccessible())
                    .bikesAllowed(gtfsTrip.getBikesAllowed())
                    .agencyId(agencyId)
                    .build();

            trips.add(trip);

            if (trips.size() >= 1000) {
                tripRepository.saveAll(trips);
                trips.clear();
            }
        }

        if (!trips.isEmpty()) {
            tripRepository.saveAll(trips);
        }

        log.info("Imported {} trips to PostgreSQL", store.getAllTrips().size());
    }

    /**
     * Import stop_times to Cassandra (high volume, time-series data)
     * Creates entries in BOTH Cassandra tables for different query patterns
     */
    private void importStopTimesToCassandra(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing stop_times to Cassandra...");

        // Build lookup maps for denormalization
        Map<String, org.onebusaway.gtfs.model.Route> routeMap = new HashMap<>();
        Map<String, org.onebusaway.gtfs.model.Trip> tripMap = new HashMap<>();

        for (org.onebusaway.gtfs.model.Route route : store.getAllRoutes()) {
            routeMap.put(route.getId().getId(), route);
        }

        for (org.onebusaway.gtfs.model.Trip trip : store.getAllTrips()) {
            tripMap.put(trip.getId().getId(), trip);
        }

        List<StopTimeCassandra> stopTimesBatch = new ArrayList<>();
        List<StopTimeByStop> stopTimesByStopBatch = new ArrayList<>();

        int count = 0;
        Instant now = Instant.now();

        for (org.onebusaway.gtfs.model.StopTime gtfsStopTime : store.getAllStopTimes()) {
            String tripId = gtfsStopTime.getTrip().getId().getId();
            org.onebusaway.gtfs.model.Trip trip = tripMap.get(tripId);
            org.onebusaway.gtfs.model.Route route = routeMap.get(trip.getRoute().getId().getId());

            // Table 1: stop_times (partitioned by trip_id)
            StopTimeCassandra stopTime = StopTimeCassandra.builder()
                    .tripId(tripId)
                    .stopSequence(gtfsStopTime.getStopSequence())
                    .stopId(gtfsStopTime.getStop().getId().getId())
                    .arrivalTime(gtfsStopTime.getArrivalTime())
                    .departureTime(gtfsStopTime.getDepartureTime())
                    .stopHeadsign(gtfsStopTime.getStopHeadsign())
                    .pickupType(gtfsStopTime.getPickupType())
                    .dropOffType(gtfsStopTime.getDropOffType())
                    .shapeDistTraveled(gtfsStopTime.getShapeDistTraveled())
                    .timepoint(gtfsStopTime.getTimepoint())
                    .agencyId(agencyId)
                    .routeId(route.getId().getId())
                    .serviceId(trip.getServiceId().getId())
                    .createdAt(now)
                    .updatedAt(now)
                    .build();

            stopTimesBatch.add(stopTime);

            // Table 2: stop_times_by_stop (partitioned by stop_id + date)
            // We need to create entries for each service date
            // For simplicity, create for "today" - in production, create for all valid service dates
            StopTimeByStop stopTimeByStop = StopTimeByStop.builder()
                    .stopId(gtfsStopTime.getStop().getId().getId())
                    .serviceDate(LocalDate.now()) // TODO: Generate for all service dates
                    .departureTime(gtfsStopTime.getDepartureTime())
                    .tripId(tripId)
                    .arrivalTime(gtfsStopTime.getArrivalTime())
                    .stopSequence(gtfsStopTime.getStopSequence())
                    .stopHeadsign(gtfsStopTime.getStopHeadsign())
                    .pickupType(gtfsStopTime.getPickupType())
                    .dropOffType(gtfsStopTime.getDropOffType())
                    // Denormalized route data
                    .routeId(route.getId().getId())
                    .routeShortName(route.getShortName())
                    .routeLongName(route.getLongName())
                    .routeColor(route.getColor())
                    .routeTextColor(route.getTextColor())
                    .routeType(route.getType())
                    // Denormalized trip data
                    .tripHeadsign(trip.getTripHeadsign())
                    .directionId(trip.getDirectionId() != null ? Integer.parseInt(trip.getDirectionId()) : null)
                    .serviceId(trip.getServiceId().getId())
                    .agencyId(agencyId)
                    .wheelchairAccessible(trip.getWheelchairAccessible())
                    .bikesAllowed(trip.getBikesAllowed())
                    .createdAt(now)
                    .build();

            stopTimesByStopBatch.add(stopTimeByStop);

            count++;

            // Batch write to Cassandra (max 5000 for safety)
            if (stopTimesBatch.size() >= 5000) {
                stopTimeCassandraRepository.saveAll(stopTimesBatch);
                stopTimeByStopRepository.saveAll(stopTimesByStopBatch);

                log.info("Wrote {} stop_times to Cassandra", count);

                stopTimesBatch.clear();
                stopTimesByStopBatch.clear();
            }
        }

        // Write remaining
        if (!stopTimesBatch.isEmpty()) {
            stopTimeCassandraRepository.saveAll(stopTimesBatch);
            stopTimeByStopRepository.saveAll(stopTimesByStopBatch);
        }

        log.info("Imported {} stop_times to Cassandra (both tables)", count);
    }

    private void importCalendar(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing calendar to PostgreSQL...");
        // Implementation similar to above
    }

    private void importCalendarDates(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing calendar dates to PostgreSQL...");
        // Implementation similar to above
    }

    private void updateAgencyImportTimestamp(String agencyId) {
        agencyRepository.findByAgencyId(agencyId).ifPresent(agency -> {
            agency.setLastGtfsImport(LocalDateTime.now());
            agencyRepository.save(agency);
        });
    }
}
