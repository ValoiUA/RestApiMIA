using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyTodoApi.Models;
public enum RoomType
{
    Small,
    Medium,
    Big,
    Large
}
public class Workspace
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("roomType")]
    [BsonRepresentation(BsonType.String)]
    public RoomType RoomType { get; set; } = RoomType.Small;

    [BsonElement("createdAt")]
    public List<DateTime>? CreatedAt { get; set; } = [];

    [BsonElement("duringDate")]
    public List<DateTime>? DuringDate { get; set; } = [];

}
