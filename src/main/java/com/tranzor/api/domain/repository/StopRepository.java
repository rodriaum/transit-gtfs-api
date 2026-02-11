package com.tranzor.api.domain.repository;

import com.tranzor.api.domain.entity.Stop;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface StopRepository extends JpaRepository<Stop, Long> {

    Optional<Stop> findByStopIdAndAgencyId(String stopId, String agencyId);

    List<Stop> findByAgencyId(String agencyId);

    Page<Stop> findByAgencyId(String agencyId, Pageable pageable);

    // Query espacial: paragens num raio de X metros
    @Query(value = """
        SELECT s.* FROM stops s
        WHERE s.agency_id = :agencyId
        AND ST_DWithin(
            s.location::geography,
            ST_MakePoint(:longitude, :latitude)::geography,
            :radiusMeters
        )
        ORDER BY ST_Distance(
            s.location::geography,
            ST_MakePoint(:longitude, :latitude)::geography
        )
        LIMIT :limit
        """, nativeQuery = true)
    List<Stop> findNearbyStops(
        @Param("latitude") Double latitude,
        @Param("longitude") Double longitude,
        @Param("radiusMeters") Double radiusMeters,
        @Param("agencyId") String agencyId,
        @Param("limit") Integer limit
    );

    // Query espacial: paragens próximas de todas as agências
    @Query(value = """
        SELECT s.* FROM stops s
        WHERE ST_DWithin(
            s.location::geography,
            ST_MakePoint(:longitude, :latitude)::geography,
            :radiusMeters
        )
        ORDER BY ST_Distance(
            s.location::geography,
            ST_MakePoint(:longitude, :latitude)::geography
        )
        LIMIT :limit
        """, nativeQuery = true)
    List<Stop> findNearbyStopsAllAgencies(
        @Param("latitude") Double latitude,
        @Param("longitude") Double longitude,
        @Param("radiusMeters") Double radiusMeters,
        @Param("limit") Integer limit
    );

    // Busca por nome
    @Query("SELECT s FROM Stop s WHERE " +
           "LOWER(s.stopName) LIKE LOWER(CONCAT('%', :searchTerm, '%')) " +
           "OR LOWER(s.stopCode) LIKE LOWER(CONCAT('%', :searchTerm, '%'))")
    Page<Stop> searchByNameOrCode(
        @Param("searchTerm") String searchTerm,
        Pageable pageable
    );

    // Busca por nome em uma agência específica
    @Query("SELECT s FROM Stop s WHERE s.agencyId = :agencyId AND " +
           "(LOWER(s.stopName) LIKE LOWER(CONCAT('%', :searchTerm, '%')) " +
           "OR LOWER(s.stopCode) LIKE LOWER(CONCAT('%', :searchTerm, '%')))")
    Page<Stop> searchByNameOrCodeAndAgency(
        @Param("searchTerm") String searchTerm,
        @Param("agencyId") String agencyId,
        Pageable pageable
    );

    // Paragens por tipo de localização
    List<Stop> findByLocationTypeAndAgencyId(Integer locationType, String agencyId);

    // Contar paragens por agência
    long countByAgencyId(String agencyId);
}
