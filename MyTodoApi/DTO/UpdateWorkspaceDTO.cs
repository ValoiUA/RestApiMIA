using MyTodoApi.Models;

namespace MyTodoApi.DTO;

public class UpdateWorkspaceDTO
{
    public bool? IsActive { get; set; }
    public RoomType? RoomType { get; set; }
}