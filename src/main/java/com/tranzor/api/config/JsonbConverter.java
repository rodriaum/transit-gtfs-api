package com.tranzor.api.config;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;
import lombok.extern.slf4j.Slf4j;

/**
 * JPA Converter for PostgreSQL JSONB type
 * Converts between String (JSON) and database JSONB
 */
@Converter
@Slf4j
public class JsonbConverter implements AttributeConverter<String, String> {

    private static final ObjectMapper objectMapper = new ObjectMapper();

    @Override
    public String convertToDatabaseColumn(String attribute) {
        if (attribute == null) {
            return null;
        }

        // Validate that the string is valid JSON
        try {
            // Parse to ensure it's valid JSON
            objectMapper.readTree(attribute);
            return attribute;
        } catch (JsonProcessingException e) {
            log.warn("Invalid JSON provided, returning null: {}", e.getMessage());
            return null;
        }
    }

    @Override
    public String convertToEntityAttribute(String dbData) {
        // PostgreSQL JSONB is returned as String by JDBC driver
        return dbData;
    }
}
