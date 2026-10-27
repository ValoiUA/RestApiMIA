using MongoDB.Driver;
using MyTodoApi.Models;

namespace MyTodoApi.Repositories;

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(IMongoDatabase database) : base(database, "users") { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email.ToLower().Trim());
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}