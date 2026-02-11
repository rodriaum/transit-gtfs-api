package com.tranzor.api.service;

import com.tranzor.api.domain.entity.cassandra.StopTimeByStop;
import com.tranzor.api.domain.repository.cassandra.StopTimeByStopRepository;
import com.tranzor.api.dto.response.DepartureDTO;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.time.LocalTime;
import java.time.temporal.ChronoUnit;
import java.util.List;
import java.util.stream.Collectors;

/**
 * DepartureService - uses Cassandra for high-volume stop_times data
 * 
 * Why Cassandra?
 * - Millions of records (largest GTFS table)
 * - Time-series data (departures by time)
 * - Write-heavy (frequent GTFS updates)
 * - Predictable query patterns (by stop_id + date + time)
 * - Horizontal scalability
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class DepartureService {

    private final StopTimeByStopRepository stopTimeByStopRepository;

    @Cacheable(value = "departures", key = "#stopId + '_' + #agencyId + '_' + #limit")
    public List<DepartureDTO> getNextDepartures(String stopId, String agencyId, Integer limit) {
        log.debug("Fetching next {} departures for stop: {} from Cassandra", limit, stopId);
        
        LocalDate currentDate = LocalDate.now();
        LocalTime currentTime = LocalTime.now();
        Integer currentTimeSeconds = timeToSeconds(currentTime);
        
        // Query Cassandra - extremely fast due to partition key + clustering
        List<StopTimeByStop> stopTimes = stopTimeByStopRepository.findNextDepartures(
            stopId, currentDate, currentTimeSeconds, limit
        );
        
        return stopTimes.stream()
            .map(st -> mapToDepartureDTO(st, currentTime))
            .collect(Collectors.toList());
    }

    @Cacheable(value = "departuresByRoute", key = "#stopId + '_' + #routeId + '_' + #agencyId + '_' + #limit")
    public List<DepartureDTO> getNextDeparturesByRoute(String stopId, String routeId, 
                                                       String agencyId, Integer limit) {
        log.debug("Fetching next {} departures for stop: {} and route: {} from Cassandra", 
                  limit, stopId, routeId);
        
        LocalDate currentDate = LocalDate.now();
        LocalTime currentTime = LocalTime.now();
        Integer currentTimeSeconds = timeToSeconds(currentTime);
        
        // Query Cassandra with route filter
        List<StopTimeByStop> stopTimes = stopTimeByStopRepository.findNextDeparturesByRoute(
            stopId, currentDate, currentTimeSeconds, routeId, limit
        );
        
        return stopTimes.stream()
            .map(st -> mapToDepartureDTO(st, currentTime))
            .collect(Collectors.toList());
    }

    public List<DepartureDTO> getScheduleForDate(String stopId, String agencyId, LocalDate date) {
        log.debug("Fetching complete schedule for stop: {} on date: {} from Cassandra", stopId, date);
        
        // Get all departures for the day from Cassandra
        List<StopTimeByStop> stopTimes = stopTimeByStopRepository.findByStopIdAndServiceDate(
            stopId, date
        );
        
        return stopTimes.stream()
            .map(st -> mapToDepartureDTO(st, null))
            .collect(Collectors.toList());
    }

    private DepartureDTO mapToDepartureDTO(StopTimeByStop stopTime, LocalTime currentTime) {
        LocalTime departureTime = stopTime.getDepartureTimeAsLocalTime();
        LocalTime arrivalTime = stopTime.getArrivalTimeAsLocalTime();
        
        Integer minutesUntil = null;
        if (currentTime != null && departureTime != null) {
            minutesUntil = (int) currentTime.until(departureTime, ChronoUnit.MINUTES);
        }
        
        return DepartureDTO.builder()
            .tripId(stopTime.getTripId())
            .routeId(stopTime.getRouteId())
            .routeShortName(stopTime.getRouteShortName())
            .routeLongName(stopTime.getRouteLongName())
            .routeColor(stopTime.getRouteColor())
            .routeTextColor(stopTime.getRouteTextColor())
            .routeType(stopTime.getRouteType())
            .departureTime(departureTime)
            .arrivalTime(arrivalTime)
            .minutesUntilDeparture(minutesUntil)
            .tripHeadsign(stopTime.getTripHeadsign())
            .stopHeadsign(stopTime.getStopHeadsign())
            .wheelchairAccessible(stopTime.getWheelchairAccessible())
            .bikesAllowed(stopTime.getBikesAllowed())
            .build();
    }

    private Integer timeToSeconds(LocalTime time) {
        return time.toSecondOfDay();
    }
}
