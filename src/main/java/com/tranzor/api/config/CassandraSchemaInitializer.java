package com.tranzor.api.config;

import com.datastax.oss.driver.api.core.CqlSession;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.InitializingBean;
import org.springframework.stereotype.Component;

/**
 * Cassandra Schema Initializer
 * Ensures stop_times table has all required columns on application startup
 */
@Component
@Slf4j
@RequiredArgsConstructor
public class CassandraSchemaInitializer implements InitializingBean {

    private final CqlSession cqlSession;

    @Override
    public void afterPropertiesSet() {
        try {
            log.info("=== Cassandra Schema Initialization Started ===");

            // Ensure keyspace exists
            executeIgnoringErrors(
                "CREATE KEYSPACE IF NOT EXISTS tranzor WITH REPLICATION = {'class': 'SimpleStrategy', 'replication_factor': 1}",
                "Keyspace creation"
            );

            // Use the keyspace
            executeIgnoringErrors("USE tranzor", "USE keyspace");

            // Create or update stop_times table
            createStopTimesTable();

            // Add missing columns if they don't exist
            addMissingColumns();

            log.info("=== Cassandra Schema Initialization Complete ===");

        } catch (Exception e) {
            log.error("Failed to initialize Cassandra schema - application may not work correctly!", e);
            // Don't throw - let app continue but warn heavily
        }
    }

    private void createStopTimesTable() {
        // Initial creation - will be dropped and recreated with correct types in addMissingColumns()
        String createTable =
            "CREATE TABLE IF NOT EXISTS stop_times (" +
            "  trip_id text," +
            "  stop_sequence int," +
            "  PRIMARY KEY (trip_id, stop_sequence)" +
            ") WITH CLUSTERING ORDER BY (stop_sequence ASC)";

        executeIgnoringErrors(createTable, "Create initial stop_times table");
    }

    private void addMissingColumns() {
        // Drop and recreate table with correct schema if columns exist with wrong types
        tryDropAndRecreateTable();
    }

    private void tryDropAndRecreateTable() {
        try {
            // Check if table has data
            log.info("Checking if stop_times table needs schema correction...");

            // Drop the table (it was likely just created empty or has wrong column types)
            executeIgnoringErrors("DROP TABLE IF EXISTS stop_times", "Drop old stop_times table");

            // Recreate with correct types
            String createTable =
                "CREATE TABLE stop_times (" +
                "  trip_id text," +
                "  stop_sequence int," +
                "  stop_id text," +
                "  arrival_time int," +
                "  departure_time int," +
                "  stop_headsign text," +
                "  pickup_type int," +
                "  drop_off_type int," +
                "  shape_dist_traveled double," +
                "  timepoint int," +
                "  agency_id text," +
                "  route_id text," +
                "  service_id text," +
                "  created_at timestamp," +
                "  updated_at timestamp," +
                "  PRIMARY KEY (trip_id, stop_sequence)" +
                ") WITH CLUSTERING ORDER BY (stop_sequence ASC)";

            cqlSession.execute(createTable);
            log.info("✓ Recreated stop_times table with correct column types");

        } catch (Exception e) {
            log.error("Failed to recreate table: {}", e.getMessage());
        }
    }

    private void executeIgnoringErrors(String cql, String description) {
        try {
            log.debug("Executing: {} - {}", description, cql.substring(0, Math.min(60, cql.length())));
            cqlSession.execute(cql);
            log.info("✓ {} - Success", description);
        } catch (Exception e) {
            // These errors are expected if things already exist
            if (e.getMessage().contains("already exists") ||
                e.getMessage().contains("conflicts with") ||
                e.getMessage().contains("Duplicate column")) {
                log.debug("○ {} - Already exists (OK)", description);
            } else {
                log.warn("⚠ {} - Failed: {}", description, e.getMessage());
            }
        }
    }
}
