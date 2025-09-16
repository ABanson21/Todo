using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Services;

public class TaskService(IDatabaseContext<TaskItem> dbContext) : BaseRepository<TaskItem>(dbContext)
{
    public async Task<List<TaskItem>> GetTasksForUsers(string userId)
    {
        var query = $"SELECT * FROM {nameof(TaskItem)} WHERE UserId = @UserId";

        var parameters = new Dictionary<string, object> { { "@UserId", userId } };

        return await dbContext.GetResultList(query, parameters);
    }
}