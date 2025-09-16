using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Services;

public class UserService(IDatabaseContext<User> dbContext) : BaseRepository<User>(dbContext)
{
    public async Task<User> UserLogin(string username, string password)
    {
        var query = $"SELECT * FROM {nameof(User)} WHERE Username = @Username AND Password = @Password";
        var parameters = new Dictionary<string, object>
        {
            { "@Username", username },
            { "@Password", password }
        };
        return await dbContext.GetResult(query, parameters);
    }
    
    public async Task<int> CheckUserExistence(string username)
    {
        var query = $"SELECT COUNT(*) FROM {nameof(User)} WHERE Username = @Username";
        var parameters = new Dictionary<string, object>
        {
            { "@Username", username },
        };
        return await dbContext.GetScalarResult<int>(query, parameters);
    }
}