package com.tranzor.api.domain.repository;

import com.tranzor.api.domain.entity.Route;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface RouteRepository extends JpaRepository<Route, Long> {

    Optional<Route> findByRouteIdAndAgencyId(String routeId, String agencyId);

    List<Route> findByAgencyId(String agencyId);

    Page<Route> findByAgencyId(String agencyId, Pageable pageable);

    List<Route> findByRouteType(Integer routeType);

    Page<Route> findByRouteTypeAndAgencyId(Integer routeType, String agencyId, Pageable pageable);

    @Query("SELECT r FROM Route r WHERE " +
           "LOWER(r.routeShortName) LIKE LOWER(CONCAT('%', :searchTerm, '%')) " +
           "OR LOWER(r.routeLongName) LIKE LOWER(CONCAT('%', :searchTerm, '%'))")
    Page<Route> searchByName(@Param("searchTerm") String searchTerm, Pageable pageable);

    @Query("SELECT r FROM Route r WHERE r.agencyId = :agencyId AND " +
           "(LOWER(r.routeShortName) LIKE LOWER(CONCAT('%', :searchTerm, '%')) " +
           "OR LOWER(r.routeLongName) LIKE LOWER(CONCAT('%', :searchTerm, '%')))")
    Page<Route> searchByNameAndAgency(
        @Param("searchTerm") String searchTerm,
        @Param("agencyId") String agencyId,
        Pageable pageable
    );

    long countByAgencyId(String agencyId);
}
