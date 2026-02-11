package com.tranzor.api.exception;

public class TripPlanningException extends RuntimeException {
    public TripPlanningException(String message) {
        super(message);
    }
    
    public TripPlanningException(String message, Throwable cause) {
        super(message, cause);
    }
}
