namespace MyTodoApi.Models;

public record BookingDTO(
    DateTime BookingDate,
    int TimeBooking,
    string? ResidentName,
    string? ResidentSurName,
    int ResidentMaxCount
);