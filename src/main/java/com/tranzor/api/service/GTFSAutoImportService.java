package com.tranzor.api.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.tranzor.api.domain.entity.Agency;
import com.tranzor.api.domain.repository.AgencyRepository;
import com.tranzor.api.domain.repository.StopRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Service;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.net.URL;
import java.nio.channels.Channels;
import java.nio.channels.ReadableByteChannel;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/**
 * GTFS Auto-Import Service
 * <p>
 * Runs automatically on application startup and:
 * 1. Loads agency configuration from JSON file
 * 2. Checks if data exists for each agency
 * 3. Checks if data is expired
 * 4. Downloads and imports GTFS if necessary
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class GTFSAutoImportService implements ApplicationRunner {

    private final AgencyRepository agencyRepository;
    private final StopRepository stopRepository;
    private final GTFSImportService gtfsImportService;

    @Value("${tranzor.gtfs.auto-import:true}")
    private boolean autoImportEnabled;

    @Value("${tranzor.gtfs.agencies-config-url:file:///data/agencies.json}")
    private String agenciesConfigUrl;

    @Value("${tranzor.gtfs.storage-path:/data/gtfs}")
    private String gtfsStoragePath;

    @Value("${tranzor.gtfs.temp-path:/tmp/gtfs}")
    private String gtfsTempPath;

    @Value("${tranzor.gtfs.expiration-days:30}")
    private int expirationDays;

    private final ObjectMapper objectMapper = new ObjectMapper();

    @Override
    public void run(ApplicationArguments args) throws Exception {
        if (!autoImportEnabled) {
            log.info("GTFS auto-import is disabled. Skipping initialization.");
            return;
        }

        log.info("=================================================");
        log.info("Starting GTFS Auto-Import Service");
        log.info("=================================================");

        try {
            // Create necessary directories
            createDirectories();

            // Load agency configuration
            List<AgencyConfig> agencyConfigs = loadAgenciesConfig();

            log.info("Found {} agencies in configuration", agencyConfigs.size());

            // Process each agency
            for (AgencyConfig config : agencyConfigs) {
                processAgency(config);
            }

            log.info("=================================================");
            log.info("GTFS Auto-Import Service completed successfully");
            log.info("=================================================");

        } catch (Exception e) {
            log.error("Error in GTFS Auto-Import Service", e);
            // Do not throw exception to prevent application startup failure
        }
    }

    private void processAgency(AgencyConfig config) {
        try {
            log.info("Processing agency: {} ({})", config.getName(), config.getAgencyId());

            // Check if agency exists in database
            Optional<Agency> existingAgency = agencyRepository.findByAgencyId(config.getAgencyId());

            boolean needsImport = false;
            String reason = "";

            if (existingAgency.isEmpty()) {
                needsImport = true;
                reason = "Agency not found in database";
            } else {
                Agency agency = existingAgency.get();

                // Check if data exists (verify if there are stops for this agency)
                long stopCount = stopRepository.countByAgencyId(config.getAgencyId());

                if (stopCount == 0) {
                    needsImport = true;
                    reason = "No data found for agency";
                } else if (agency.getLastGtfsImport() == null) {
                    needsImport = true;
                    reason = "No import timestamp found";
                } else if (isDataExpired(agency)) {
                    needsImport = true;
                    reason = String.format("Data expired (last import: %s)", agency.getLastGtfsImport());
                } else {
                    log.info("Agency {} has valid data. Last import: {}",
                            config.getAgencyId(), agency.getLastGtfsImport());
                }
            }

            if (needsImport) {
                log.info("Import needed for agency {}: {}", config.getAgencyId(), reason);

                // Save or update agency before import
                saveOrUpdateAgency(config, existingAgency);

                // Download and import GTFS
                importGTFSForAgency(config);
            }

        } catch (Exception e) {
            log.error("Error processing agency {}: {}", config.getAgencyId(), e.getMessage(), e);
        }
    }

    private boolean isDataExpired(Agency agency) {
        // Check if expiration date is configured
        if (agency.getGtfsExpireAt() != null) {
            return LocalDateTime.now().isAfter(agency.getGtfsExpireAt());
        }

        // If not, use last import date + expirationDays
        if (agency.getLastGtfsImport() != null) {
            LocalDateTime expirationDate = agency.getLastGtfsImport().plusDays(expirationDays);
            return LocalDateTime.now().isAfter(expirationDate);
        }

        return true;
    }

    private void saveOrUpdateAgency(AgencyConfig config, Optional<Agency> existingAgency) {
        Agency agency = existingAgency.orElse(new Agency());

        agency.setAgencyId(config.getAgencyId());
        agency.setName(config.getName());
        agency.setGtfsUrl(config.getGtfsUrl());
        agency.setImageUrl(config.getImageUrl());

        // Process expiration date
        if (config.getGtfsExpireAt() != null && !config.getGtfsExpireAt().isEmpty()) {
            try {
                DateTimeFormatter formatter = DateTimeFormatter.ofPattern("dd-MM-yyyy");
                agency.setGtfsExpireAt(LocalDateTime.parse(config.getGtfsExpireAt() + " 00:00:00",
                        DateTimeFormatter.ofPattern("dd-MM-yyyy HH:mm:ss")));
            } catch (DateTimeParseException e) {
                log.warn("Could not parse gtfs_expire_at for {}: {}", config.getAgencyId(), config.getGtfsExpireAt());
            }
        }

        if (agency.getActive() == null) {
            agency.setActive(true);
        }

        // Set default values if not present
        if (agency.getTimezone() == null) {
            agency.setTimezone("Europe/Lisbon");
        }

        agencyRepository.save(agency);
        log.info("Agency {} saved/updated in database", config.getAgencyId());
    }

    private void importGTFSForAgency(AgencyConfig config) {
        try {
            // Download GTFS file
            File gtfsFile = downloadGTFS(config);

            if (gtfsFile != null && gtfsFile.exists()) {
                log.info("Starting GTFS import for agency: {}", config.getAgencyId());

                // Import using the existing service
                gtfsImportService.importGTFS(config.getAgencyId(), gtfsFile);

                log.info("GTFS import completed for agency: {}", config.getAgencyId());

                // Clean up temporary file
                if (gtfsFile.delete()) {
                    log.debug("Temporary GTFS file deleted: {}", gtfsFile.getAbsolutePath());
                }
            } else {
                log.error("Failed to download GTFS file for agency: {}", config.getAgencyId());
            }

        } catch (Exception e) {
            log.error("Error importing GTFS for agency {}: {}", config.getAgencyId(), e.getMessage(), e);
        }
    }

    private File downloadGTFS(AgencyConfig config) {
        try {
            String gtfsUrl = config.getGtfsUrl();

            if (gtfsUrl == null || gtfsUrl.isEmpty()) {
                log.warn("No GTFS URL configured for agency: {}", config.getAgencyId());
                return null;
            }

            log.info("Downloading GTFS from: {}", gtfsUrl);

            // Create temporary file
            File tempDir = new File(gtfsTempPath);

            // If path is relative, use project root
            if (!tempDir.isAbsolute()) {
                String projectRoot = System.getProperty("user.dir");
                tempDir = new File(projectRoot, gtfsTempPath);
            }

            if (!tempDir.exists()) {
                tempDir.mkdirs();
            }

            File tempFile = new File(tempDir, config.getAgencyId() + "_" + System.currentTimeMillis() + ".zip");

            // Download file
            URL url = new URL(gtfsUrl);
            try (ReadableByteChannel rbc = Channels.newChannel(url.openStream());
                 FileOutputStream fos = new FileOutputStream(tempFile)) {

                fos.getChannel().transferFrom(rbc, 0, Long.MAX_VALUE);
            }

            log.info("GTFS file downloaded: {} ({} bytes)", tempFile.getAbsolutePath(), tempFile.length());
            return tempFile;

        } catch (Exception e) {
            log.error("Error downloading GTFS for agency {}: {}", config.getAgencyId(), e.getMessage(), e);
            return null;
        }
    }

    private List<AgencyConfig> loadAgenciesConfig() throws Exception {
        List<AgencyConfig> configs = new ArrayList<>();

        log.info("Loading agencies configuration from: {}", agenciesConfigUrl);

        File configFile;

        // Support file:// URLs or absolute/relative paths
        if (agenciesConfigUrl.startsWith("file://")) {
            String path = agenciesConfigUrl.substring(7);
            configFile = new File(path);
        } else {
            configFile = new File(agenciesConfigUrl);
        }

        // If the file doesn't exist and it's a relative path, try from project root
        if (!configFile.exists() && !configFile.isAbsolute()) {
            String projectRoot = System.getProperty("user.dir");
            configFile = new File(projectRoot, agenciesConfigUrl);
            log.debug("Trying relative path from project root: {}", configFile.getAbsolutePath());
        }

        if (!configFile.exists()) {
            log.warn("Agencies configuration file not found: {}", configFile.getAbsolutePath());
            return configs;
        }

        try (FileInputStream fis = new FileInputStream(configFile)) {
            JsonNode root = objectMapper.readTree(fis);

            if (root.isArray()) {
                for (JsonNode node : root) {
                    AgencyConfig config = new AgencyConfig();
                    config.setAgencyId(node.get("agency_id").asText());
                    config.setName(node.get("name").asText());
                    config.setGtfsUrl(node.has("gtfs_url") ? node.get("gtfs_url").asText() : null);
                    config.setImageUrl(node.has("image_url") ? node.get("image_url").asText() : null);
                    config.setGtfsExpireAt(node.has("gtfs_expire_at") ? node.get("gtfs_expire_at").asText() : null);

                    configs.add(config);
                }
            }
        }

        log.info("Loaded {} agency configurations", configs.size());
        return configs;
    }

    private void createDirectories() {
        File storageDir = new File(gtfsStoragePath);
        File tempDir = new File(gtfsTempPath);

        // If paths are relative, use project root
        if (!storageDir.isAbsolute()) {
            String projectRoot = System.getProperty("user.dir");
            storageDir = new File(projectRoot, gtfsStoragePath);
        }

        if (!tempDir.isAbsolute()) {
            String projectRoot = System.getProperty("user.dir");
            tempDir = new File(projectRoot, gtfsTempPath);
        }

        if (!storageDir.exists()) {
            storageDir.mkdirs();
            log.info("Created GTFS storage directory: {}", storageDir.getAbsolutePath());
        }

        if (!tempDir.exists()) {
            tempDir.mkdirs();
            log.info("Created GTFS temp directory: {}", tempDir.getAbsolutePath());
        }
    }

    /**
     * Internal class for agency configuration
     */
    @lombok.Data
    private static class AgencyConfig {
        private String agencyId;
        private String name;
        private String gtfsUrl;
        private String imageUrl;
        private String gtfsExpireAt;
    }
}
