using MyTodoApi.Repositories;

namespace MyTodoApi.Services;

public class BookingSyncService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ILogger<BookingSyncService> _logger;

    public BookingSyncService(
        IBookingRepository bookingRepository,
        IWorkspaceRepository workspaceRepository,
        ILogger<BookingSyncService> logger)
    {
        _bookingRepository = bookingRepository;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    public async Task CleanupExpiredBookingsAsync()
    {
        _logger.LogInformation("Початок синхронізації та очищення прострочених бронювань...");

        var nowUtc = DateTime.UtcNow;
        var allBookings = await _bookingRepository.GetAllAsync();
        var allWorkspaces = await _workspaceRepository.GetAllAsync();

        var expiredBookings = allBookings
            .Where(b => 
            {
                var bookingUtc = b.BookingDate.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(b.BookingDate, DateTimeKind.Utc) 
                    : b.BookingDate.ToUniversalTime();

                return bookingUtc.AddMinutes(b.TimeBooking) <= nowUtc;
            })
            .ToList();

        _logger.LogInformation("Знайдено прострочених бронювань: {Count}", expiredBookings.Count);

        foreach (var booking in expiredBookings)
        {
            var workspace = allWorkspaces.FirstOrDefault(w => w.Id == booking.WorkspaceId);
            if (workspace is not null)
            {
                var bookingEnd = booking.BookingDate.AddMinutes(booking.TimeBooking);

                workspace.CreatedAt?.RemoveAll(d => d == booking.BookingDate);
                workspace.DuringDate?.RemoveAll(d => d == bookingEnd);

                await _workspaceRepository.UpdateAsync(workspace.Id!, workspace);
            }

            await _bookingRepository.DeleteAsync(booking.Id!);
            _logger.LogInformation("Видалено прострочене бронювання ID: {Id}", booking.Id);
        }

        foreach (var ws in allWorkspaces)
        {
            bool modified = false;
            if (ws.DuringDate is not null && ws.DuringDate.Count > 0)
            {
                for (int i = ws.DuringDate.Count - 1; i >= 0; i--)
                {
                    var dateUtc = ws.DuringDate[i].Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(ws.DuringDate[i], DateTimeKind.Utc) 
                        : ws.DuringDate[i].ToUniversalTime();

                    if (dateUtc <= nowUtc)
                    {
                        ws.DuringDate.RemoveAt(i);
                        if (ws.CreatedAt is not null && ws.CreatedAt.Count > i)
                        {
                            ws.CreatedAt.RemoveAt(i);
                        }
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                await _workspaceRepository.UpdateAsync(ws.Id!, ws);
            }
        }

        _logger.LogInformation("Синхронізацію успішно завершено.");
    }
}