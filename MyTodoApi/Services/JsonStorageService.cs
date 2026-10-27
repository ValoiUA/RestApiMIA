using System.Text.Json;
using System.Text.Json.Serialization;
using MyTodoApi.Models;

namespace MyTodoApi.Services;

public class JsonHelper
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _basePath;

    public JsonHelper()
    {
        _basePath = Directory.GetCurrentDirectory();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<List<T>> ReadFromJsonFileAsync<T>(string fileName)
    {
        var path = Path.Combine(_basePath, fileName);
        if (!File.Exists(path))
        {
            return new List<T>();
        }

        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonOptions) ?? new List<T>();
    }

    public async Task WriteToJsonFileAsync<T>(string fileName, List<T> data)
    {
        var path = Path.Combine(_basePath, fileName);
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, data, _jsonOptions);
    }

    public async Task SyncUnActiveBookingAsync()
    {
        var bookings = await ReadFromJsonFileAsync<Booking>("booking.json");
        bookings.RemoveAll(b => DateTime.Now > b.BookingDate);

        await WriteToJsonFileAsync("booking.json", bookings);
    }

    public async Task SyncActiveRoomsAsync()
    {
        var workspaces = await ReadFromJsonFileAsync<Workspace>("workspace.json");
        var bookings = await ReadFromJsonFileAsync<Booking>("booking.json");

        foreach (var workspace in workspaces)
        {
            workspace.IsActive = true;
            workspace.CreatedAt = [];
            workspace.DuringDate = [];
        }

        foreach (var workspace in workspaces)
        {
            foreach (var booking in bookings)
            {
                if (booking.WorkspaceId == workspace.Id)
                {
                    var bookingStart = booking.BookingDate;
                    var bookingEnd = booking.BookingDate.AddMinutes(booking.TimeBooking);

                    if (DateTime.Now >= bookingStart && DateTime.Now <= bookingEnd)
                    {
                        workspace.IsActive = false;
                        break;
                    }

                    workspace.CreatedAt.Add(booking.BookingDate);
                    workspace.DuringDate.Add(bookingEnd);
                }
            }
        }

        await WriteToJsonFileAsync("workspace.json", workspaces);
    }
}