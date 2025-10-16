using Microsoft.EntityFrameworkCore;
using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Repository;

public class UserRepository(AppDatabaseContext dbContext, ILogger<UserRepository> logger)
{
    public async Task<User?> GetUserByUsername(string username)
    {
        return await dbContext.User.FirstOrDefaultAsync(x => x.UserName == username);
    }
    
    public async Task<User?> GetUserByUserId(int userId)
    {
        return await dbContext.User.FindAsync(userId);
    }
    
    public async Task<List<User>> GetAllUsers()
    {
        return await dbContext.User.ToListAsync();
    }
    
    public async Task UpdateUser(User user)
    {
        dbContext.User.Update(user);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task DeleteUser(int userId)
    {
        var user = await dbContext.User.FindAsync(userId);
        if (user != null)
        {
            dbContext.User.Remove(user);
            await dbContext.SaveChangesAsync();
        }
    }
    
    public async Task CreateUser(User user)
    {
        await dbContext.User.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}