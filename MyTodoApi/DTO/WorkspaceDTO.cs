namespace MyTodoApi.DTO;

using MyTodoApi.Models;

public record WorkspaceDTO(
    bool? IsActive,
    RoomType? RoomType,
    List<DateTime>? CreatedAt,
    List<DateTime>? DuringDate
);