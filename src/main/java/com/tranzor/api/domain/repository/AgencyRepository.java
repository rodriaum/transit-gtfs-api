package com.tranzor.api.domain.repository;

import com.tranzor.api.domain.entity.Agency;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface AgencyRepository extends JpaRepository<Agency, Long> {

    Optional<Agency> findByAgencyId(String agencyId);

    List<Agency> findByActiveTrue();

    boolean existsByAgencyId(String agencyId);
}
