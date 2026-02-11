package com.tranzor.api.dto.response;

import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.*;

import java.time.LocalDateTime;
import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class TripPlanDTO {
    
    @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ss")
    private LocalDateTime requestTime;
    
    private LocationDTO from;
    private LocationDTO to;
    
    private List<ItineraryDTO> itineraries;
    
    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class LocationDTO {
        private String name;
        private Double lat;
        private Double lon;
        private String stopId;
    }
    
    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class ItineraryDTO {
        private Long durationSeconds;
        
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ss")
        private LocalDateTime startTime;
        
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ss")
        private LocalDateTime endTime;
        
        private Integer walkDistance;
        private Integer transitTime;
        private Integer waitingTime;
        private Integer walkTime;
        private Integer transfers;
        
        private List<LegDTO> legs;
    }
    
    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class LegDTO {
        
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ss")
        private LocalDateTime startTime;
        
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ss")
        private LocalDateTime endTime;
        
        private String mode; // WALK, BUS, SUBWAY, RAIL, etc
        private String routeShortName;
        private String routeLongName;
        private String routeColor;
        private String tripHeadsign;
        private String agencyName;
        
        private LocationDTO from;
        private LocationDTO to;
        
        private Integer distance;
        private Long duration;
        
        // Polyline da rota (encoded)
        private String legGeometry;
        
        // Lista de paragens intermediárias
        private List<String> intermediateStops;
    }
}
