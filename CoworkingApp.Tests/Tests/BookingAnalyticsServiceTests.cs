namespace CoworkingApp.Tests.Tests;

using Moq;
using MyTodoApi.Models;
using MyTodoApi.Repositories;
using MyTodoApi.Services;
using Xunit;

public class BookingAnalyticsServiceTests
{
    private readonly Mock<IBookingRepository> _mockBookingRepo;
    private readonly BookingAnalyticsService _analyticsService;

    public BookingAnalyticsServiceTests()
    {
        // Створюємо Mock для інтерфейсу репозиторію
        _mockBookingRepo = new Mock<IBookingRepository>();

        // Передаємо mockRepo.Object у конструктор тестованого сервісу
        _analyticsService = new BookingAnalyticsService(_mockBookingRepo.Object);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_ValidBookings_ReturnsCorrectCalculations()
    {
        // Arrange
        var testBookings = new List<Booking>
        {
            new() { Id = "1", BookingDate = new DateTime(2026, 10, 5), TotalPrice = 300m, TimeBooking = 120 },
            new() { Id = "2", BookingDate = new DateTime(2026, 10, 15), TotalPrice = 500m, TimeBooking = 60 },
            new() { Id = "3", BookingDate = new DateTime(2026, 9, 20), TotalPrice = 1000m, TimeBooking = 240 } // Інший місяць
        };

        _mockBookingRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(testBookings);

        // Act
        var report = await _analyticsService.GetMonthlyReportAsync(2026, 10);

        // Assert: перевірка агрегацій LINQ
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalBookingsCount);
        Assert.Equal(800m, report.TotalRevenue);
        Assert.Equal(400m, report.AverageBookingPrice);
        Assert.Equal(90.0, report.AverageDurationMinutes);

        // Assert: факт виклику репозиторію
        _mockBookingRepo.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_EmptyCollection_ReturnsZeroStatistics()
    {
        // Arrange: порожня колекція з бази даних
        _mockBookingRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Booking>());

        // Act
        var report = await _analyticsService.GetMonthlyReportAsync(2026, 10);

        // Assert: граничний випадок без ділення на нуль
        Assert.Equal(0, report.TotalBookingsCount);
        Assert.Equal(0m, report.TotalRevenue);
        Assert.Equal(0m, report.AverageBookingPrice);
        Assert.Equal(0.0, report.AverageDurationMinutes);
    }

    [Theory]
    [InlineData(1999, 5)]
    [InlineData(2101, 5)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task GetMonthlyReportAsync_InvalidDateParameters_ThrowsArgumentOutOfRangeException(int year, int month)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _analyticsService.GetMonthlyReportAsync(year, month)
        );

        // Переконуємось, що репозиторій навіть не смикався, якщо валідація впала
        _mockBookingRepo.Verify(repo => repo.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetHighestValueBookingAsync_WhenBookingsExist_ReturnsBookingWithMaxPrice()
    {
        // Arrange
        var testBookings = new List<Booking>
        {
            new() { Id = "1", ResidentMaxCount = 3, TotalPrice = 250m },
            new() { Id = "2", ResidentMaxCount = 5, TotalPrice = 900m },
            new() { Id = "3", ResidentMaxCount = 2, TotalPrice = 400m }
        };

        _mockBookingRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(testBookings);

        // Act
        var highest = await _analyticsService.GetHighestValueBookingAsync(minGuests: 2);

        // Assert
        Assert.NotNull(highest);
        Assert.Equal("2", highest.Id);
        Assert.Equal(900m, highest.TotalPrice);
    }

    [Fact]
    public async Task GetHighestValueBookingAsync_NoMatchingCriteria_ThrowsInvalidOperationException()
    {
        // Arrange
        var testBookings = new List<Booking>
        {
            new() { Id = "1", ResidentMaxCount = 2, TotalPrice = 150m }
        };

        _mockBookingRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(testBookings);

        // Act & Assert (критерій minGuests = 10, якого немає)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _analyticsService.GetHighestValueBookingAsync(minGuests: 10)
        );
    }

    [Fact]
    public async Task CountLongBookingsByResidentAsync_MatchesCondition_ReturnsCorrectCount()
    {
        // Arrange
        var testBookings = new List<Booking>
        {
            new() { ResidentSurName = "Шевченко", TimeBooking = 180 }, // 3 години
            new() { ResidentSurName = "шевченко", TimeBooking = 120 }, // 2 години (регістронезалежно)
            new() { ResidentSurName = "Шевченко", TimeBooking = 30 },  // 0.5 години (менше порогу)
            new() { ResidentSurName = "Коваленко", TimeBooking = 300 } // інший резидент
        };

        _mockBookingRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(testBookings);

        // Act (поріг 2 години = 120 хв)
        int count = await _analyticsService.CountLongBookingsByResidentAsync("Шевченко", thresholdHours: 2);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CancelBookingByIdAsync_BookingExists_CallsRepositoryDelete()
    {
        // Arrange
        string bookingId = "book-123";
        var existingBooking = new Booking { Id = bookingId, ResidentName = "Олег" };

        _mockBookingRepo.Setup(repo => repo.GetByIdAsync(bookingId))
            .ReturnsAsync(existingBooking);

        _mockBookingRepo.Setup(repo => repo.DeleteAsync(bookingId))
    .ReturnsAsync(true);

        // Act
        await _analyticsService.CancelBookingByIdAsync(bookingId);

        // Assert: перевіряємо, що репозиторій викликав метод GetById і Delete рівно один раз
        _mockBookingRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        _mockBookingRepo.Verify(repo => repo.DeleteAsync(bookingId), Times.Once);
    }

    [Fact]
    public async Task CancelBookingByIdAsync_BookingNotFound_ThrowsKeyNotFoundExceptionAndNeverDeletes()
    {
        // Arrange
        string nonExistentId = "missing-404";
        _mockBookingRepo.Setup(repo => repo.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Booking?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _analyticsService.CancelBookingByIdAsync(nonExistentId)
        );

        // Перевіряємо, що метод Delete ніколи не викликався, якщо сутності не знайдено
        _mockBookingRepo.Verify(repo => repo.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}