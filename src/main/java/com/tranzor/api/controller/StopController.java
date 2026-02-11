package com.tranzor.api.controller;

import com.tranzor.api.dto.response.StopDTO;
import com.tranzor.api.service.StopService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/stops")
@RequiredArgsConstructor
@Tag(name = "Stops", description = "Endpoints para consultar paragens/estações")
public class StopController {

    private final StopService stopService;

    @GetMapping("/{stopId}")
    @Operation(summary = "Obter detalhes de uma paragem", 
               description = "Retorna informações detalhadas de uma paragem específica")
    public ResponseEntity<StopDTO> getStop(
            @Parameter(description = "ID da paragem", required = true)
            @PathVariable String stopId,
            
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId) {
        
        return ResponseEntity.ok(stopService.getStopById(stopId, agencyId));
    }

    @GetMapping("/nearby")
    @Operation(summary = "Procurar paragens próximas", 
               description = "Retorna paragens num raio específico baseado em coordenadas GPS")
    public ResponseEntity<List<StopDTO>> getNearbyStops(
            @Parameter(description = "Latitude", required = true, example = "41.1496")
            @RequestParam Double latitude,
            
            @Parameter(description = "Longitude", required = true, example = "-8.6109")
            @RequestParam Double longitude,
            
            @Parameter(description = "Raio em metros", example = "500")
            @RequestParam(defaultValue = "500") Double radiusMeters,
            
            @Parameter(description = "ID da agência (opcional)")
            @RequestParam(required = false) String agencyId,
            
            @Parameter(description = "Número máximo de resultados", example = "10")
            @RequestParam(defaultValue = "10") Integer limit) {
        
        return ResponseEntity.ok(
            stopService.findNearbyStops(latitude, longitude, radiusMeters, agencyId, limit)
        );
    }

    @GetMapping("/search")
    @Operation(summary = "Pesquisar paragens", 
               description = "Pesquisa paragens por nome ou código")
    public ResponseEntity<Page<StopDTO>> searchStops(
            @Parameter(description = "Termo de pesquisa", required = true)
            @RequestParam String q,
            
            @Parameter(description = "ID da agência (opcional)")
            @RequestParam(required = false) String agencyId,
            
            @Parameter(description = "Número da página", example = "0")
            @RequestParam(defaultValue = "0") int page,
            
            @Parameter(description = "Tamanho da página", example = "20")
            @RequestParam(defaultValue = "20") int size) {
        
        Pageable pageable = PageRequest.of(page, size);
        return ResponseEntity.ok(stopService.searchStops(q, agencyId, pageable));
    }

    @GetMapping
    @Operation(summary = "Listar paragens por agência", 
               description = "Retorna todas as paragens de uma agência com paginação")
    public ResponseEntity<Page<StopDTO>> getStopsByAgency(
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId,
            
            @Parameter(description = "Número da página", example = "0")
            @RequestParam(defaultValue = "0") int page,
            
            @Parameter(description = "Tamanho da página", example = "20")
            @RequestParam(defaultValue = "20") int size) {
        
        Pageable pageable = PageRequest.of(page, size);
        return ResponseEntity.ok(stopService.getStopsByAgency(agencyId, pageable));
    }
}
