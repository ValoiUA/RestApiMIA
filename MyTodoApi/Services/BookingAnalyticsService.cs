namespace MyTodoApi.Services;

using MyTodoApi.Models;
using MyTodoApi.Repositories;

public class MonthlySummaryReport
{
    public int TotalBookingsCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingPrice { get; set; }
}

public class BookingAnalyticsService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingAnalyticsService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
    }

    public async Task<MonthlySummaryReport> GetMonthlyReportAsync(int year, int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            throw new ArgumentOutOfRangeException("Некоректний період.");

        var allBookings = await _bookingRepository.GetAllAsync();
        var monthly = allBookings.Where(b => b.BookingDate.Year == year && b.BookingDate.Month == month).ToList();

        if (!monthly.Any())
            return new MonthlySummaryReport();

        return new MonthlySummaryReport
        {
            TotalBookingsCount = monthly.Count,
            TotalRevenue = monthly.Sum(b => b.TotalPrice),
            AverageBookingPrice = Math.Round(monthly.Average(b => b.TotalPrice), 2)
        };
    }

    public async Task<Booking> GetHighestValueBookingAsync(int minGuests = 1)
    {
        if (minGuests < 1)
            throw new ArgumentException("Мінімум 1 гість.", nameof(minGuests));

        var bookings = await _bookingRepository.GetAllAsync();
        var matching = bookings.Where(b => b.ResidentMaxCount >= minGuests).ToList();

        if (!matching.Any())
            throw new InvalidOperationException("Бронювань за критерієм не знайдено.");

        return matching.OrderByDescending(b => b.TotalPrice).First();
    }

    public async Task CancelBookingByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id обов'язковий.");

        var existing = await _bookingRepository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Бронювання {id} не існує.");

        await _bookingRepository.DeleteAsync(id);
    }
}