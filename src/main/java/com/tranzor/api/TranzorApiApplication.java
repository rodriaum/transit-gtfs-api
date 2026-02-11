package com.tranzor.api;

import io.github.cdimascio.dotenv.Dotenv;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.scheduling.annotation.EnableScheduling;

import java.util.TimeZone;

@SpringBootApplication
@EnableCaching
@EnableScheduling
public class TranzorApiApplication {

    public static void main(String[] args) {
        try {
            Dotenv dotenv = Dotenv.configure()
                    .ignoreIfMissing()
                    .load();

            dotenv.entries().forEach(entry -> {
                String key = entry.getKey();
                String value = entry.getValue();

                if (System.getenv(key) == null && System.getProperty(key) == null) {
                    System.setProperty(key, value);
                }
            });

            System.out.println("Environment variables loaded from .env file");
        } catch (Exception e) {
            System.out.println("Could not load .env file: " + e.getMessage() + ". Using system environment variables.");
        }

        TimeZone.setDefault(TimeZone.getTimeZone("Europe/Lisbon"));

        SpringApplication.run(TranzorApiApplication.class, args);
    }
}
