using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Repository;

public class UserRepository(IDatabaseContext<User> dbContext) : BaseRepository<User>(dbContext)
{
    public async Task<User> CheckUserExistence(string username)
    {
        var query = $"SELECT * FROM {nameof(User)} WHERE UserName = @Username";
        var parameters = new Dictionary<string, object>
        {
            { "@Username", username },
        };
        return await dbContext.GetResult(query, parameters);
    }
}