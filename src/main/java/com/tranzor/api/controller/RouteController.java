package com.tranzor.api.controller;

import com.tranzor.api.dto.response.RouteDTO;
import com.tranzor.api.service.RouteService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/routes")
@RequiredArgsConstructor
@Tag(name = "Routes", description = "Endpoints para consultar rotas/linhas de transporte")
public class RouteController {

    private final RouteService routeService;

    @GetMapping("/{routeId}")
    @Operation(summary = "Obter detalhes de uma rota", 
               description = "Retorna informações detalhadas de uma rota/linha específica")
    public ResponseEntity<RouteDTO> getRoute(
            @Parameter(description = "ID da rota", required = true)
            @PathVariable String routeId,
            
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId) {
        
        return ResponseEntity.ok(routeService.getRouteById(routeId, agencyId));
    }

    @GetMapping
    @Operation(summary = "Listar rotas por agência", 
               description = "Retorna todas as rotas de uma agência com paginação")
    public ResponseEntity<Page<RouteDTO>> getRoutesByAgency(
            @Parameter(description = "ID da agência", required = true)
            @RequestParam String agencyId,
            
            @Parameter(description = "Número da página", example = "0")
            @RequestParam(defaultValue = "0") int page,
            
            @Parameter(description = "Tamanho da página", example = "20")
            @RequestParam(defaultValue = "20") int size) {
        
        Pageable pageable = PageRequest.of(page, size);
        return ResponseEntity.ok(routeService.getRoutesByAgency(agencyId, pageable));
    }

    @GetMapping("/by-type")
    @Operation(summary = "Listar rotas por tipo", 
               description = "Retorna rotas filtradas por tipo de transporte (0=Tram, 1=Metro, 2=Rail, 3=Bus, etc)")
    public ResponseEntity<Page<RouteDTO>> getRoutesByType(
            @Parameter(description = "Tipo de rota (0-12)", required = true, example = "3")
            @RequestParam Integer routeType,
            
            @Parameter(description = "ID da agência (opcional)")
            @RequestParam(required = false) String agencyId,
            
            @Parameter(description = "Número da página", example = "0")
            @RequestParam(defaultValue = "0") int page,
            
            @Parameter(description = "Tamanho da página", example = "20")
            @RequestParam(defaultValue = "20") int size) {
        
        Pageable pageable = PageRequest.of(page, size);
        return ResponseEntity.ok(routeService.getRoutesByType(routeType, agencyId, pageable));
    }

    @GetMapping("/search")
    @Operation(summary = "Pesquisar rotas", 
               description = "Pesquisa rotas por nome ou número")
    public ResponseEntity<Page<RouteDTO>> searchRoutes(
            @Parameter(description = "Termo de pesquisa", required = true)
            @RequestParam String q,
            
            @Parameter(description = "ID da agência (opcional)")
            @RequestParam(required = false) String agencyId,
            
            @Parameter(description = "Número da página", example = "0")
            @RequestParam(defaultValue = "0") int page,
            
            @Parameter(description = "Tamanho da página", example = "20")
            @RequestParam(defaultValue = "20") int size) {
        
        Pageable pageable = PageRequest.of(page, size);
        return ResponseEntity.ok(routeService.searchRoutes(q, agencyId, pageable));
    }
}
