package com.tranzor.api.config;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Contact;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.info.License;
import io.swagger.v3.oas.models.servers.Server;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.List;

@Configuration
public class OpenAPIConfig {

    @Bean
    public OpenAPI tranzorOpenAPI() {
        Server devServer = new Server();
        devServer.setUrl("http://localhost:8080/api/v1/tranzor");
        devServer.setDescription("Servidor de Desenvolvimento");

        Contact contact = new Contact();
        contact.setName("Tranzor API Team");
        contact.setEmail("api@tranzor.com");

        License mitLicense = new License()
            .name("MIT License")
            .url("https://choosealicense.com/licenses/mit/");

        Info info = new Info()
            .title("Tranzor Public Transport API")
            .version("1.0.0")
            .contact(contact)
            .description("API completa para consulta de transportes públicos em Portugal. " +
                        "Permite consultar horários, paragens, rotas, próximas partidas e " +
                        "planeamento de viagens baseado em dados GTFS.")
            .termsOfService("https://tranzor.com/terms")
            .license(mitLicense);

        return new OpenAPI()
            .info(info)
            .servers(List.of(devServer));
    }
}
