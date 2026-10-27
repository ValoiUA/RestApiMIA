namespace MyTodoApi.DTO;

using MyTodoApi.Models;

public record ResidentDTO(
    string Name,
    string SurName,
    ResidentType? Type
);