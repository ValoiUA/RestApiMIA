using MongoDB.Driver;
using MyTodoApi.Models;

namespace MyTodoApi.Repositories;

public class WorkspaceRepository : MongoRepository<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(IMongoDatabase database) : base(database, "workspaces") { }
}