package com.tranzor.api.controller;

import com.tranzor.api.dto.response.TripPlanDTO;
import com.tranzor.api.service.TripPlannerService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;

@RestController
@RequestMapping("/trip-planner")
@RequiredArgsConstructor
@Tag(name = "Trip Planner", description = "Endpoints para planeamento de viagens e cálculo de rotas")
public class TripPlannerController {

    private final TripPlannerService tripPlannerService;

    @GetMapping("/plan")
    @Operation(summary = "Calcular rota entre dois pontos", 
               description = "Calcula a melhor rota entre origem e destino usando OpenTripPlanner")
    public ResponseEntity<TripPlanDTO> planTrip(
            @Parameter(description = "Latitude de origem", required = true, example = "41.1496")
            @RequestParam Double fromLat,
            
            @Parameter(description = "Longitude de origem", required = true, example = "-8.6109")
            @RequestParam Double fromLon,
            
            @Parameter(description = "Latitude de destino", required = true, example = "41.1579")
            @RequestParam Double toLat,
            
            @Parameter(description = "Longitude de destino", required = true, example = "-8.6291")
            @RequestParam Double toLon,
            
            @Parameter(description = "Data e hora de partida (formato: yyyy-MM-dd'T'HH:mm:ss)", 
                      example = "2026-02-11T09:00:00")
            @RequestParam(required = false) 
            @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) LocalDateTime departureTime,
            
            @Parameter(description = "Modo de transporte (TRANSIT,WALK / BUS,WALK / etc)", 
                      example = "TRANSIT,WALK")
            @RequestParam(required = false, defaultValue = "TRANSIT,WALK") String mode,
            
            @Parameter(description = "Distância máxima a pé em metros", example = "1000")
            @RequestParam(required = false) Integer maxWalkDistance,
            
            @Parameter(description = "Acessível para cadeiras de rodas", example = "false")
            @RequestParam(required = false) Boolean wheelchair) {
        
        TripPlanDTO plan = tripPlannerService.planTrip(
            fromLat, fromLon, toLat, toLon, 
            departureTime, mode, maxWalkDistance, wheelchair
        );
        
        return ResponseEntity.ok(plan);
    }
}
