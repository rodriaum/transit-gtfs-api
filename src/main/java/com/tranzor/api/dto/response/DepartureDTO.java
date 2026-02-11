package com.tranzor.api.dto.response;

import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.*;

import java.time.LocalTime;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class DepartureDTO {
    
    private String tripId;
    private String routeId;
    private String routeShortName;
    private String routeLongName;
    private String routeColor;
    private String routeTextColor;
    private Integer routeType;
    
    @JsonFormat(pattern = "HH:mm:ss")
    private LocalTime departureTime;
    
    @JsonFormat(pattern = "HH:mm:ss")
    private LocalTime arrivalTime;
    
    private Integer minutesUntilDeparture;
    private String tripHeadsign;
    private String stopHeadsign;
    private Integer wheelchairAccessible;
    private Integer bikesAllowed;
    
    private StopDTO stop;
    
    // Dados de realtime, se disponíveis
    private RealtimeInfoDTO realtime;
    
    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class RealtimeInfoDTO {
        private Integer delay; // em segundos
        private String vehicleId;
        private Double vehicleLat;
        private Double vehicleLon;
        private String occupancyStatus;
        private LocalTime estimatedArrival;
        private LocalTime estimatedDeparture;
    }
}
