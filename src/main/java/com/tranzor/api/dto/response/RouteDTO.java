package com.tranzor.api.dto.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.*;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class RouteDTO {
    
    private String routeId;
    private String agencyId;
    private String agencyName;
    private String routeShortName;
    private String routeLongName;
    private String routeDesc;
    private Integer routeType;
    private String routeTypeDescription;
    private String routeUrl;
    private String routeColor;
    private String routeTextColor;
    private Integer routeSortOrder;
    
    public String getRouteTypeDescription() {
        if (routeType == null) return null;
        
        return switch (routeType) {
            case 0 -> "Tram, Streetcar, Light rail";
            case 1 -> "Subway, Metro";
            case 2 -> "Rail";
            case 3 -> "Bus";
            case 4 -> "Ferry";
            case 5 -> "Cable tram";
            case 6 -> "Aerial lift, suspended cable car";
            case 7 -> "Funicular";
            case 11 -> "Trolleybus";
            case 12 -> "Monorail";
            default -> "Other";
        };
    }
}
