package com.tranzor.api.dto.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.*;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class StopDTO {
    
    private String stopId;
    private String stopCode;
    private String stopName;
    private String stopDesc;
    private Double latitude;
    private Double longitude;
    private String zoneId;
    private String stopUrl;
    private Integer locationType;
    private String parentStation;
    private String stopTimezone;
    private Integer wheelchairBoarding;
    private String platformCode;
    private String agencyId;
    private String agencyName;
    
    // Para queries de proximidade
    private Double distanceMeters;
}
