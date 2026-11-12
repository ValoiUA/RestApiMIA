using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CoworkingApp.Tests.Infrastructure;
using MyTodoApi.Models;
using Xunit;

namespace CoworkingApp.Tests.Tests
{
    public class BookingControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly InMemoryBookingRepository _repo;

        public BookingControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
            _repo = factory.TestRepository;
        }

        public Task InitializeAsync()
        {
            _repo.Clear();
            return Task.CompletedTask;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetAll_Returns200Ok_AndListOfBookings()
        {
            _repo.Seed(new Booking
            {
                Id = "int-test-1",
                ResidentName = "Олександр",
                ResidentSurName = "Петренко",
                BookingDate = DateTime.UtcNow,
                TimeBooking = 60,
                ResidentMaxCount = 2,
                TotalPrice = 200m
            });

            var response = await _client.GetAsync("/api/booking");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<List<Booking>>();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Олександр", result[0].ResidentName);
        }

        [Fact]
        public async Task Create_ValidData_Returns201Created_AndSavesToDb()
        {
            var newBooking = new Booking
            {
                ResidentName = "Іван",
                ResidentSurName = "Франко",
                BookingDate = DateTime.UtcNow.AddDays(1),
                TimeBooking = 120,
                ResidentMaxCount = 3,
                TotalPrice = 400m
            };

            var response = await _client.PostAsJsonAsync("/api/booking", newBooking);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var all = await _repo.GetAllAsync();
            Assert.Contains(all, b => b.ResidentName == "Іван" && b.ResidentSurName == "Франко");
        }

        [Fact]
        public async Task Create_InvalidData_Returns400BadRequest()
        {
            var invalidBooking = new Booking
            {
                ResidentName = "",
                TimeBooking = -60,
                ResidentMaxCount = 0
            };

            var response = await _client.PostAsJsonAsync("/api/booking", invalidBooking);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ExistingBooking_Returns200Ok_AndRemovesFromDb()
        {
            _repo.Seed(new Booking
            {
                Id = "delete-id-100",
                ResidentName = "Тарас",
                ResidentSurName = "Шевченко",
                BookingDate = DateTime.UtcNow.AddDays(2),
                TimeBooking = 60,
                ResidentMaxCount = 1,
                TotalPrice = 200m
            });

            var response = await _client.DeleteAsync("/api/booking/delete-id-100");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var item = await _repo.GetByIdAsync("delete-id-100");
            Assert.Null(item);
        }

        [Fact]
        public async Task Delete_NonExistingBooking_Returns404NotFound()
        {
            var response = await _client.DeleteAsync("/api/booking/non-existing-id-999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
