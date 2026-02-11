package com.tranzor.api.service;

import com.tranzor.api.domain.entity.*;
import com.tranzor.api.domain.repository.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.locationtech.jts.geom.Coordinate;
import org.locationtech.jts.geom.GeometryFactory;
import org.locationtech.jts.geom.Point;
import org.locationtech.jts.geom.PrecisionModel;
import org.onebusaway.gtfs.impl.GtfsRelationalDaoImpl;
import org.onebusaway.gtfs.model.*;
import org.onebusaway.gtfs.serialization.GtfsReader;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.io.File;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
@Slf4j
public class GTFSImportService {

    private final AgencyRepository agencyRepository;
    private final StopRepository stopRepository;
    private final RouteRepository routeRepository;
    private final TripRepository tripRepository;
    private final StopTimeRepository stopTimeRepository;
    
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
            
            // Importar na ordem correta devido a dependências
            importAgencies(store, agencyId);
            importStops(store, agencyId);
            importRoutes(store, agencyId);
            importTrips(store, agencyId);
            importStopTimes(store, agencyId);
            importCalendar(store, agencyId);
            importCalendarDates(store, agencyId);
            
            // Atualizar timestamp de importação
            updateAgencyImportTimestamp(agencyId);
            
            log.info("GTFS import completed successfully for agency: {}", agencyId);
            
        } catch (Exception e) {
            log.error("Error importing GTFS for agency: {}", agencyId, e);
            throw new RuntimeException("Failed to import GTFS data", e);
        }
    }

    private void importAgencies(GtfsRelationalDaoImpl store, String mainAgencyId) {
        log.info("Importing agencies...");
        
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
    }

    private void importStops(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing stops for agency: {}...", agencyId);
        
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
            
            // Salvar em lotes para melhor performance
            if (stops.size() >= 1000) {
                stopRepository.saveAll(stops);
                stops.clear();
            }
        }
        
        if (!stops.isEmpty()) {
            stopRepository.saveAll(stops);
        }
        
        log.info("Imported {} stops", store.getAllStops().size());
    }

    private void importRoutes(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing routes for agency: {}...", agencyId);
        
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
        
        log.info("Imported {} routes", store.getAllRoutes().size());
    }

    private void importTrips(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing trips for agency: {}...", agencyId);
        
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
        
        log.info("Imported {} trips", store.getAllTrips().size());
    }

    private void importStopTimes(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing stop times for agency: {}...", agencyId);
        
        List<StopTime> stopTimes = new ArrayList<>();
        
        for (org.onebusaway.gtfs.model.StopTime gtfsStopTime : store.getAllStopTimes()) {
            StopTime stopTime = StopTime.builder()
                .tripId(gtfsStopTime.getTrip().getId().getId())
                .arrivalTime(gtfsStopTime.getArrivalTime())
                .departureTime(gtfsStopTime.getDepartureTime())
                .stopId(gtfsStopTime.getStop().getId().getId())
                .stopSequence(gtfsStopTime.getStopSequence())
                .stopHeadsign(gtfsStopTime.getStopHeadsign())
                .pickupType(gtfsStopTime.getPickupType())
                .dropOffType(gtfsStopTime.getDropOffType())
                .shapeDistTraveled(gtfsStopTime.getShapeDistTraveled())
                .timepoint(gtfsStopTime.getTimepoint())
                .agencyId(agencyId)
                .build();
            
            stopTimes.add(stopTime);
            
            if (stopTimes.size() >= 5000) {
                stopTimeRepository.saveAll(stopTimes);
                stopTimes.clear();
            }
        }
        
        if (!stopTimes.isEmpty()) {
            stopTimeRepository.saveAll(stopTimes);
        }
        
        log.info("Imported {} stop times", store.getAllStopTimes().size());
    }

    private void importCalendar(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing calendar for agency: {}...", agencyId);
        // Implementation similar to above
    }

    private void importCalendarDates(GtfsRelationalDaoImpl store, String agencyId) {
        log.info("Importing calendar dates for agency: {}...", agencyId);
        // Implementation similar to above
    }

    private void updateAgencyImportTimestamp(String agencyId) {
        agencyRepository.findByAgencyId(agencyId).ifPresent(agency -> {
            agency.setLastGtfsImport(LocalDateTime.now());
            agencyRepository.save(agency);
        });
    }
}
