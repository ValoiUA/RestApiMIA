using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MyTodoApi.Models;
using MyTodoApi.Repositories;

namespace CoworkingApp.Tests.Infrastructure
{
    public class InMemoryBookingRepository : IBookingRepository
    {
        private readonly ConcurrentDictionary<string, Booking> _store = new();

        public Task<List<Booking>> GetAllAsync() =>
            Task.FromResult(_store.Values.ToList());

        public Task<Booking?> GetByIdAsync(string id)
        {
            _store.TryGetValue(id, out var booking);
            return Task.FromResult(booking);
        }

        public Task<List<Booking>> FindAsync(Expression<Func<Booking, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var matches = _store.Values.Where(compiled).ToList();
            return Task.FromResult(matches);
        }

        public Task CreateAsync(Booking booking)
        {
            if (string.IsNullOrWhiteSpace(booking.Id))
            {
                booking.Id = Guid.NewGuid().ToString();
            }
            _store[booking.Id] = booking;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(string id, Booking booking)
        {
            if (!_store.ContainsKey(id)) return Task.FromResult(false);
            _store[id] = booking;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string id) =>
            Task.FromResult(_store.TryRemove(id, out _));

        public void Clear() => _store.Clear();

        public void Seed(Booking booking) => _store[booking.Id] = booking;
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public InMemoryBookingRepository TestRepository { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBookingRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IBookingRepository>(TestRepository);
            });
        }
    }
}
