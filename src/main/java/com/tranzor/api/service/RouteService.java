package com.tranzor.api.service;

import com.tranzor.api.domain.entity.Route;
import com.tranzor.api.domain.repository.RouteRepository;
import com.tranzor.api.dto.response.RouteDTO;
import com.tranzor.api.exception.ResourceNotFoundException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
@Slf4j
@Transactional(readOnly = true)
public class RouteService {

    private final RouteRepository routeRepository;

    @Cacheable(value = "routes", key = "#routeId + '_' + #agencyId")
    public RouteDTO getRouteById(String routeId, String agencyId) {
        log.debug("Fetching route: {} for agency: {}", routeId, agencyId);
        
        Route route = routeRepository.findByRouteIdAndAgencyId(routeId, agencyId)
            .orElseThrow(() -> new ResourceNotFoundException(
                String.format("Route %s not found for agency %s", routeId, agencyId)
            ));
        
        return mapToDTO(route);
    }

    public Page<RouteDTO> getRoutesByAgency(String agencyId, Pageable pageable) {
        log.debug("Fetching routes for agency: {}", agencyId);
        
        return routeRepository.findByAgencyId(agencyId, pageable)
            .map(this::mapToDTO);
    }

    public Page<RouteDTO> getRoutesByType(Integer routeType, String agencyId, Pageable pageable) {
        log.debug("Fetching routes of type: {} for agency: {}", routeType, agencyId);
        
        if (agencyId != null && !agencyId.isEmpty()) {
            return routeRepository.findByRouteTypeAndAgencyId(routeType, agencyId, pageable)
                .map(this::mapToDTO);
        } else {
            return routeRepository.findAll(pageable)
                .map(this::mapToDTO);
        }
    }

    public Page<RouteDTO> searchRoutes(String searchTerm, String agencyId, Pageable pageable) {
        log.debug("Searching routes with term: {} for agency: {}", searchTerm, agencyId);
        
        if (agencyId != null && !agencyId.isEmpty()) {
            return routeRepository.searchByNameAndAgency(searchTerm, agencyId, pageable)
                .map(this::mapToDTO);
        } else {
            return routeRepository.searchByName(searchTerm, pageable)
                .map(this::mapToDTO);
        }
    }

    private RouteDTO mapToDTO(Route route) {
        RouteDTO dto = RouteDTO.builder()
            .routeId(route.getRouteId())
            .agencyId(route.getAgencyId())
            .routeShortName(route.getRouteShortName())
            .routeLongName(route.getRouteLongName())
            .routeDesc(route.getRouteDesc())
            .routeType(route.getRouteType())
            .routeUrl(route.getRouteUrl())
            .routeColor(route.getRouteColor())
            .routeTextColor(route.getRouteTextColor())
            .routeSortOrder(route.getRouteSortOrder())
            .build();
        
        dto.setRouteTypeDescription(dto.getRouteTypeDescription());
        
        return dto;
    }
}
