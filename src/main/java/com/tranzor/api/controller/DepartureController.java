package com.tranzor.api.controller;

import com.tranzor.api.dto.response.DepartureDTO;
import com.tranzor.api.service.DepartureService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/departures")
@RequiredArgsConstructor
@Tag(name = "Departures", description = "Endpoints para consultar próximas partidas e horários")
public class DepartureController {

    private final DepartureService departureService;

    @GetMapping
    @Operation(summary = "Consultar próximas partidas de uma paragem", 
               description = "Retorna as próximas partidas de uma paragem específica em tempo real")
    public ResponseEntity<List<DepartureDTO>> getNextDepartures(
            @Parameter(description = "ID da paragem", required = true)
            @RequestParam String stopId,
            
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId,
            
            @Parameter(description = "Número máximo de partidas", example = "10")
            @RequestParam(defaultValue = "10") Integer limit) {
        
        return ResponseEntity.ok(
            departureService.getNextDepartures(stopId, agencyId, limit)
        );
    }

    @GetMapping("/by-route")
    @Operation(summary = "Consultar próximas partidas de uma rota específica", 
               description = "Retorna as próximas partidas filtradas por rota")
    public ResponseEntity<List<DepartureDTO>> getNextDeparturesByRoute(
            @Parameter(description = "ID da paragem", required = true)
            @RequestParam String stopId,
            
            @Parameter(description = "ID da rota", required = true)
            @RequestParam String routeId,
            
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId,
            
            @Parameter(description = "Número máximo de partidas", example = "10")
            @RequestParam(defaultValue = "10") Integer limit) {
        
        return ResponseEntity.ok(
            departureService.getNextDeparturesByRoute(stopId, routeId, agencyId, limit)
        );
    }

    @GetMapping("/schedule")
    @Operation(summary = "Consultar horário completo de uma paragem", 
               description = "Retorna todos os horários de uma paragem para uma data específica")
    public ResponseEntity<List<DepartureDTO>> getScheduleForDate(
            @Parameter(description = "ID da paragem", required = true)
            @RequestParam String stopId,
            
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId,
            
            @Parameter(description = "Data (formato: yyyy-MM-dd)", example = "2026-02-11")
            @RequestParam @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate date) {
        
        return ResponseEntity.ok(
            departureService.getScheduleForDate(stopId, agencyId, date)
        );
    }
}
