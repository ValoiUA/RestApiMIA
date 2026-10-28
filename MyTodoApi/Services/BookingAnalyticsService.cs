namespace MyTodoApi.Services;

using MyTodoApi.Models;
using MyTodoApi.Repositories;

public class MonthlySummaryReport
{
    public int TotalBookingsCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingPrice { get; set; }
    public double AverageDurationMinutes { get; set; }
}

public class BookingAnalyticsService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingAnalyticsService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
    }

    // 1. Агрегація доходів та середніх показників за вказаний місяць/рік через LINQ
    public async Task<MonthlySummaryReport> GetMonthlyReportAsync(int year, int month)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Рік має бути в діапазоні від 2000 до 2100.");

        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Номер місяця має бути від 1 до 12.");

        var allBookings = await _bookingRepository.GetAllAsync();

        var monthlyBookings = allBookings
            .Where(b => b.BookingDate.Year == year && b.BookingDate.Month == month)
            .ToList();

        if (!monthlyBookings.Any())
        {
            return new MonthlySummaryReport
            {
                TotalBookingsCount = 0,
                TotalRevenue = 0m,
                AverageBookingPrice = 0m,
                AverageDurationMinutes = 0
            };
        }

        return new MonthlySummaryReport
        {
            TotalBookingsCount = monthlyBookings.Count,
            TotalRevenue = monthlyBookings.Sum(b => b.TotalPrice),
            AverageBookingPrice = Math.Round(monthlyBookings.Average(b => b.TotalPrice), 2),
            AverageDurationMinutes = Math.Round(monthlyBookings.Average(b => b.TimeBooking), 2)
        };
    }

    // 2. Пошук бронювання з максимальною вартістю за певною умовою (LINQ MaxBy / OrderByDescending)
    public async Task<Booking> GetHighestValueBookingAsync(int minGuests = 1)
    {
        if (minGuests < 1)
            throw new ArgumentException("Мінімальна кількість гостей має бути не менше 1.", nameof(minGuests));

        var bookings = await _bookingRepository.GetAllAsync();

        var targetBookings = bookings.Where(b => b.ResidentMaxCount >= minGuests).ToList();

        if (!targetBookings.Any())
            throw new InvalidOperationException("Не знайдено жодного бронювання за заданими критеріями.");

        return targetBookings.OrderByDescending(b => b.TotalPrice).First();
    }

    // 3. Підрахунок кількості тривалих бронювань (понад N годин) для резидента
    public async Task<int> CountLongBookingsByResidentAsync(string residentSurname, int thresholdHours)
    {
        if (string.IsNullOrWhiteSpace(residentSurname))
            throw new ArgumentException("Прізвище резидента не може бути порожнім.", nameof(residentSurname));

        if (thresholdHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(thresholdHours), "Поріг годин має бути додатним.");

        var allBookings = await _bookingRepository.GetAllAsync();

        int thresholdMinutes = thresholdHours * 60;

        return allBookings
            .Count(b => b.ResidentSurName.Equals(residentSurname, StringComparison.OrdinalIgnoreCase) 
                        && b.TimeBooking >= thresholdMinutes);
    }

    // 4. Безпечне видалення з обов'язковою верифікацією існування
    public async Task CancelBookingByIdAsync(string bookingId)
    {
        if (string.IsNullOrWhiteSpace(bookingId))
            throw new ArgumentException("Ідентифікатор бронювання обов'язковий.", nameof(bookingId));

        var existing = await _bookingRepository.GetByIdAsync(bookingId);
        if (existing == null)
            throw new KeyNotFoundException($"Бронювання з ID '{bookingId}' не знайдено.");

        await _bookingRepository.DeleteAsync(bookingId);
    }
}