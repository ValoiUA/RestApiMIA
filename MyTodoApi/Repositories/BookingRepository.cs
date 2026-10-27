using MongoDB.Driver;
using MyTodoApi.Models;

namespace MyTodoApi.Repositories;

public class BookingRepository : MongoRepository<Booking>, IBookingRepository
{
    public BookingRepository(IMongoDatabase database) : base(database, "bookings") { }
}