namespace CoworkingApp.Tests.Tests;

using Moq;
using MyTodoApi.Models;
using MyTodoApi.Repositories;
using MyTodoApi.Services;
using Xunit;

public class BookingAnalyticsServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepo = new();
    private readonly BookingAnalyticsService _service;

    public BookingAnalyticsServiceTests()
    {
        _service = new BookingAnalyticsService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_ValidBookings_ReturnsCorrectCalculations()
    {
        // Arrange
        var testData = new List<Booking>
        {
            new() { Id = "1", BookingDate = new DateTime(2026, 10, 5), TotalPrice = 300m },
            new() { Id = "2", BookingDate = new DateTime(2026, 10, 15), TotalPrice = 500m },
            new() { Id = "3", BookingDate = new DateTime(2026, 9, 1), TotalPrice = 1000m }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(testData);

        // Act
        var report = await _service.GetMonthlyReportAsync(2026, 10);

        // Assert
        Assert.Equal(2, report.TotalBookingsCount);
        Assert.Equal(800m, report.TotalRevenue);
        Assert.Equal(400m, report.AverageBookingPrice);
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_EmptyCollection_ReturnsZero()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

        var report = await _service.GetMonthlyReportAsync(2026, 10);

        Assert.Equal(0, report.TotalBookingsCount);
        Assert.Equal(0m, report.TotalRevenue);
    }

    [Fact]
    public async Task GetHighestValueBookingAsync_WhenExists_ReturnsMaxPriceBooking()
    {
        var testData = new List<Booking>
        {
            new() { Id = "1", ResidentMaxCount = 3, TotalPrice = 250m },
            new() { Id = "2", ResidentMaxCount = 5, TotalPrice = 900m }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(testData);

        var highest = await _service.GetHighestValueBookingAsync(minGuests: 2);

        Assert.Equal("2", highest.Id);
        Assert.Equal(900m, highest.TotalPrice);
    }

    [Fact]
    public async Task CancelBookingByIdAsync_BookingNotFound_ThrowsKeyNotFoundException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CancelBookingByIdAsync("missing"));
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingByIdAsync_Exists_CallsDelete()
    {
        _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(new Booking { Id = "1" });
        _mockRepo.Setup(r => r.DeleteAsync("1")).ReturnsAsync(true);

        await _service.CancelBookingByIdAsync("1");

        _mockRepo.Verify(r => r.DeleteAsync("1"), Times.Once);
    }
}