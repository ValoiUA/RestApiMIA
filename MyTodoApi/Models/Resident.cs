using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace MyTodoApi.Models;
public enum ResidentType
{
    Regular,
    VIP,
    Guest
}
public class Resident
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("surname")]
    public string SurName { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ResidentType? Type { get; set; } = null;
}
