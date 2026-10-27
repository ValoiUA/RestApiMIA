namespace MyTodoApi.DTO;

public class UpdateBookingDTO
{
    public DateTime? BookingDate { get; set; }
    public int? TimeBooking { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentSurName { get; set; }
    public int? ResidentMaxCount { get; set; }
}