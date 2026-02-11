package com.tranzor.api.service;

import com.tranzor.api.domain.entity.Stop;
import com.tranzor.api.domain.repository.StopRepository;
import com.tranzor.api.dto.response.StopDTO;
import com.tranzor.api.exception.ResourceNotFoundException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Slf4j
@Transactional(readOnly = true)
public class StopService {

    private final StopRepository stopRepository;

    @Cacheable(value = "stops", key = "#stopId + '_' + #agencyId")
    public StopDTO getStopById(String stopId, String agencyId) {
        log.debug("Fetching stop: {} for agency: {}", stopId, agencyId);
        
        Stop stop = stopRepository.findByStopIdAndAgencyId(stopId, agencyId)
            .orElseThrow(() -> new ResourceNotFoundException(
                String.format("Stop %s not found for agency %s", stopId, agencyId)
            ));
        
        return mapToDTO(stop);
    }

    @Cacheable(value = "nearbyStops", key = "#latitude + '_' + #longitude + '_' + #radiusMeters + '_' + #agencyId")
    public List<StopDTO> findNearbyStops(Double latitude, Double longitude, 
                                         Double radiusMeters, String agencyId, 
                                         Integer limit) {
        log.debug("Finding stops within {}m of ({}, {}) for agency {}", 
                  radiusMeters, latitude, longitude, agencyId);
        
        validateCoordinates(latitude, longitude);
        validateRadius(radiusMeters);
        
        List<Stop> stops;
        if (agencyId != null && !agencyId.isEmpty()) {
            stops = stopRepository.findNearbyStops(
                latitude, longitude, radiusMeters, agencyId, limit
            );
        } else {
            stops = stopRepository.findNearbyStopsAllAgencies(
                latitude, longitude, radiusMeters, limit
            );
        }
        
        return stops.stream()
            .map(this::mapToDTO)
            .collect(Collectors.toList());
    }

    public Page<StopDTO> searchStops(String searchTerm, String agencyId, Pageable pageable) {
        log.debug("Searching stops with term: {} for agency: {}", searchTerm, agencyId);
        
        Page<Stop> stops;
        if (agencyId != null && !agencyId.isEmpty()) {
            stops = stopRepository.searchByNameOrCodeAndAgency(searchTerm, agencyId, pageable);
        } else {
            stops = stopRepository.searchByNameOrCode(searchTerm, pageable);
        }
        
        return stops.map(this::mapToDTO);
    }

    public Page<StopDTO> getStopsByAgency(String agencyId, Pageable pageable) {
        log.debug("Fetching stops for agency: {}", agencyId);
        
        return stopRepository.findByAgencyId(agencyId, pageable)
            .map(this::mapToDTO);
    }

    private StopDTO mapToDTO(Stop stop) {
        return StopDTO.builder()
            .stopId(stop.getStopId())
            .stopCode(stop.getStopCode())
            .stopName(stop.getStopName())
            .stopDesc(stop.getStopDesc())
            .latitude(stop.getStopLat())
            .longitude(stop.getStopLon())
            .zoneId(stop.getZoneId())
            .stopUrl(stop.getStopUrl())
            .locationType(stop.getLocationType())
            .parentStation(stop.getParentStation())
            .stopTimezone(stop.getStopTimezone())
            .wheelchairBoarding(stop.getWheelchairBoarding())
            .platformCode(stop.getPlatformCode())
            .agencyId(stop.getAgencyId())
            .build();
    }

    private void validateCoordinates(Double latitude, Double longitude) {
        if (latitude == null || longitude == null) {
            throw new IllegalArgumentException("Latitude and longitude are required");
        }
        if (latitude < -90 || latitude > 90) {
            throw new IllegalArgumentException("Latitude must be between -90 and 90");
        }
        if (longitude < -180 || longitude > 180) {
            throw new IllegalArgumentException("Longitude must be between -180 and 180");
        }
    }

    private void validateRadius(Double radiusMeters) {
        if (radiusMeters == null || radiusMeters <= 0) {
            throw new IllegalArgumentException("Radius must be greater than 0");
        }
        if (radiusMeters > 5000) {
            throw new IllegalArgumentException("Radius cannot exceed 5000 meters");
        }
    }
}
