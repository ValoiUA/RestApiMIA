using MongoDB.Driver;
using MyTodoApi.Models;

namespace MyTodoApi.Repositories;

public class ResidentRepository : MongoRepository<Resident>, IResidentRepository
{
    public ResidentRepository(IMongoDatabase database) : base(database, "residents") { }
}