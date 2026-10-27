using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace MyTodoApi.Models;

public class Booking
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id {get; set; }

    [BsonElement("bookingDate")]
    public DateTime BookingDate {get; set; } = DateTime.Now;

    [BsonElement("timeBooking")]
    public int TimeBooking {get; set; } = 60;

    [BsonElement("residentName")]
    public string? ResidentName {get; set; } = null;

    [BsonElement("residentSurName")]
    public string? ResidentSurName {get; set; } = null;

    [BsonElement("residentMaxCount")]
    public int ResidentMaxCount {get; set;} = 1;

    [BsonElement("workspaceId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? WorkspaceId {get; set;}

    [BsonElement("totalPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPrice {get; set;}

}