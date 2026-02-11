package com.tranzor.api.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.tranzor.api.dto.response.TripPlanDTO;
import com.tranzor.api.exception.TripPlanningException;
import io.github.resilience4j.circuitbreaker.annotation.CircuitBreaker;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.http.*;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.util.UriComponentsBuilder;

import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
@Slf4j
public class TripPlannerService {

    @Value("${tranzor.otp.base-url}")
    private String otpBaseUrl;

    private final RestTemplate restTemplate = new RestTemplate();
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Cacheable(value = "tripPlans", key = "#fromLat + '_' + #fromLon + '_' + #toLat + '_' + #toLon")
    @CircuitBreaker(name = "otp", fallbackMethod = "planTripFallback")
    public TripPlanDTO planTrip(Double fromLat, Double fromLon, 
                               Double toLat, Double toLon,
                               LocalDateTime departureTime,
                               String mode,
                               Integer maxWalkDistance,
                               Boolean wheelchair) {
        
        log.debug("Planning trip from ({},{}) to ({},{})", fromLat, fromLon, toLat, toLon);
        
        validateCoordinates(fromLat, fromLon);
        validateCoordinates(toLat, toLon);
        
        String url = buildOtpUrl(fromLat, fromLon, toLat, toLon, 
                                departureTime, mode, maxWalkDistance, wheelchair);
        
        try {
            ResponseEntity<String> response = restTemplate.exchange(
                url,
                HttpMethod.GET,
                null,
                String.class
            );
            
            if (response.getStatusCode() == HttpStatus.OK && response.getBody() != null) {
                return parseOtpResponse(response.getBody(), fromLat, fromLon, toLat, toLon);
            }
            
            throw new TripPlanningException("Failed to get response from OTP");
            
        } catch (Exception e) {
            log.error("Error planning trip with OTP", e);
            throw new TripPlanningException("Failed to plan trip: " + e.getMessage(), e);
        }
    }

    private String buildOtpUrl(Double fromLat, Double fromLon, 
                               Double toLat, Double toLon,
                               LocalDateTime departureTime,
                               String mode,
                               Integer maxWalkDistance,
                               Boolean wheelchair) {
        
        UriComponentsBuilder builder = UriComponentsBuilder
            .fromHttpUrl(otpBaseUrl + "/routers/default/plan")
            .queryParam("fromPlace", fromLat + "," + fromLon)
            .queryParam("toPlace", toLat + "," + toLon)
            .queryParam("mode", mode != null ? mode : "TRANSIT,WALK")
            .queryParam("maxWalkDistance", maxWalkDistance != null ? maxWalkDistance : 1000)
            .queryParam("wheelchair", wheelchair != null ? wheelchair : false)
            .queryParam("numItineraries", 3);
        
        if (departureTime != null) {
            builder.queryParam("date", departureTime.format(DateTimeFormatter.ISO_LOCAL_DATE))
                   .queryParam("time", departureTime.format(DateTimeFormatter.ofPattern("HH:mm:ss")));
        }
        
        return builder.toUriString();
    }

    private TripPlanDTO parseOtpResponse(String jsonResponse, 
                                        Double fromLat, Double fromLon,
                                        Double toLat, Double toLon) throws Exception {
        
        JsonNode root = objectMapper.readTree(jsonResponse);
        JsonNode plan = root.path("plan");
        
        if (plan.isMissingNode()) {
            throw new TripPlanningException("No plan found in OTP response");
        }
        
        List<TripPlanDTO.ItineraryDTO> itineraries = new ArrayList<>();
        JsonNode itinerariesNode = plan.path("itineraries");
        
        for (JsonNode itineraryNode : itinerariesNode) {
            itineraries.add(parseItinerary(itineraryNode));
        }
        
        return TripPlanDTO.builder()
            .requestTime(LocalDateTime.now())
            .from(TripPlanDTO.LocationDTO.builder()
                .lat(fromLat)
                .lon(fromLon)
                .build())
            .to(TripPlanDTO.LocationDTO.builder()
                .lat(toLat)
                .lon(toLon)
                .build())
            .itineraries(itineraries)
            .build();
    }

    private TripPlanDTO.ItineraryDTO parseItinerary(JsonNode node) {
        long duration = node.path("duration").asLong();
        long startTime = node.path("startTime").asLong();
        long endTime = node.path("endTime").asLong();
        int walkDistance = node.path("walkDistance").asInt();
        int transitTime = node.path("transitTime").asInt();
        int waitingTime = node.path("waitingTime").asInt();
        int walkTime = node.path("walkTime").asInt();
        int transfers = node.path("transfers").asInt();
        
        List<TripPlanDTO.LegDTO> legs = new ArrayList<>();
        JsonNode legsNode = node.path("legs");
        
        for (JsonNode legNode : legsNode) {
            legs.add(parseLeg(legNode));
        }
        
        return TripPlanDTO.ItineraryDTO.builder()
            .durationSeconds(duration)
            .startTime(LocalDateTime.ofEpochSecond(startTime / 1000, 0, java.time.ZoneOffset.UTC))
            .endTime(LocalDateTime.ofEpochSecond(endTime / 1000, 0, java.time.ZoneOffset.UTC))
            .walkDistance(walkDistance)
            .transitTime(transitTime)
            .waitingTime(waitingTime)
            .walkTime(walkTime)
            .transfers(transfers)
            .legs(legs)
            .build();
    }

    private TripPlanDTO.LegDTO parseLeg(JsonNode node) {
        long startTime = node.path("startTime").asLong();
        long endTime = node.path("endTime").asLong();
        String mode = node.path("mode").asText();
        int distance = node.path("distance").asInt();
        long duration = node.path("duration").asLong();
        
        JsonNode fromNode = node.path("from");
        JsonNode toNode = node.path("to");
        
        TripPlanDTO.LegDTO.LegDTOBuilder builder = TripPlanDTO.LegDTO.builder()
            .startTime(LocalDateTime.ofEpochSecond(startTime / 1000, 0, java.time.ZoneOffset.UTC))
            .endTime(LocalDateTime.ofEpochSecond(endTime / 1000, 0, java.time.ZoneOffset.UTC))
            .mode(mode)
            .distance(distance)
            .duration(duration)
            .from(parseLocation(fromNode))
            .to(parseLocation(toNode))
            .legGeometry(node.path("legGeometry").path("points").asText());
        
        // Adicionar informações de rota se disponível
        if (!mode.equals("WALK")) {
            builder.routeShortName(node.path("routeShortName").asText(null))
                   .routeLongName(node.path("routeLongName").asText(null))
                   .routeColor(node.path("routeColor").asText(null))
                   .tripHeadsign(node.path("headsign").asText(null))
                   .agencyName(node.path("agencyName").asText(null));
        }
        
        return builder.build();
    }

    private TripPlanDTO.LocationDTO parseLocation(JsonNode node) {
        return TripPlanDTO.LocationDTO.builder()
            .name(node.path("name").asText())
            .lat(node.path("lat").asDouble())
            .lon(node.path("lon").asDouble())
            .stopId(node.path("stopId").asText(null))
            .build();
    }

    // Fallback method para circuit breaker
    public TripPlanDTO planTripFallback(Double fromLat, Double fromLon, 
                                       Double toLat, Double toLon,
                                       LocalDateTime departureTime,
                                       String mode,
                                       Integer maxWalkDistance,
                                       Boolean wheelchair,
                                       Exception e) {
        log.error("Trip planning failed, using fallback", e);
        throw new TripPlanningException("Trip planning service is temporarily unavailable");
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
}
