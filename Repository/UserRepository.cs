using Microsoft.EntityFrameworkCore;
using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Repository;

public class UserRepository(AppDatabaseContext dbContext, ILogger<UserRepository> logger)
{
    public async Task<User?> CheckUserExistence(string username)
    {
        return  await dbContext.User.FirstOrDefaultAsync(x => x.UserName == username);
    }
    
    public async Task<User?> GetUser(int userId)
    {
        return await dbContext.User.FirstOrDefaultAsync(x => x.Id == userId);
    }
    
    public async Task<List<User>> GetAllUsers()
    {
        return await dbContext.User.ToListAsync();
    }
    
    public async Task CreateUser(User user)
    {
        await dbContext.User.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}