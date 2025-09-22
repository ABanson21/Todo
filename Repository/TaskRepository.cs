using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Repository;

public class TaskRepository(IDatabaseContext<TaskItem> dbContext) : BaseRepository<TaskItem>(dbContext)
{
    protected override Task CreateTable()
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS TaskItem (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT,
                Description VARCHAR(255),
                DueDate DATETIME,
                ModifiedDate DATETIME,
                IsCompleted BOOLEAN,
                FOREIGN KEY (UserId) REFERENCES User(Id) ON DELETE CASCADE
            )";

        return dbContext.ExecuteAction(createTableSql);

    }

    public async Task<List<TaskItem>> GetTasksForUsers(string userId)
    {
        var query = $"SELECT * FROM {nameof(TaskItem)} WHERE UserId = @UserId";

        var parameters = new Dictionary<string, object> { { "@UserId", userId } };

        return await dbContext.GetResultList(query, parameters);
    }
}