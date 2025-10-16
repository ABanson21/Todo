// using Microsoft.EntityFrameworkCore;
// using TodoBackend.Database;
// using TodoBackend.Model;
//
// namespace TodoBackend.Repository;
//
// public class TaskRepository(AppDatabaseContext dbContext, ILogger<TaskRepository> logger)
//
// {
//     public async Task UpdateTask(TaskItem task)
//     {
//         var taskEntity = await dbContext.TaskItem.FirstOrDefaultAsync(x => x.Id == task.Id);
//         if (taskEntity == null)
//         {
//             throw new ArgumentException("Task not found");
//         }
//         taskEntity.Description = task.Description;
//         taskEntity.DueDate = task.DueDate;
//         taskEntity.IsCompleted = task.IsCompleted;
//         taskEntity.ModifiedDate = DateTime.UtcNow;
//         await dbContext.SaveChangesAsync();
//     }
//     
//     public async Task CreateTask(TaskItem task)
//     {
//         task.IsCompleted = false;
//         task.ModifiedDate = DateTime.UtcNow;
//         
//         await dbContext.TaskItem.AddAsync(task);
//         await dbContext.SaveChangesAsync();
//     }
//     
//     public async Task<List<TaskItem>> GetTasksForUsers(string userId)
//     {
//         if (int.TryParse(userId, out var userIdInteger))
//         {
//             return await dbContext.TaskItem.Where(x => x.UserId == userIdInteger).ToListAsync();
//         }
//         throw new ArgumentException("Invalid userId");
//     }
//     
//     public async Task<List<TaskItem>> GetAllTasks()
//     {
//         return await dbContext.TaskItem.ToListAsync();
//     }
//     
//     public async Task DeleteSingleTask(int taskId)
//     {
//         var taskToRemove = dbContext.TaskItem.FirstOrDefault(t => t.Id == taskId); 
//         if (taskToRemove == null)
//         {
//             throw new ArgumentException("Task not found");
//         }
//         dbContext.TaskItem.Remove(taskToRemove);
//         await dbContext.SaveChangesAsync();
//     }
//     
//     public async Task DeleteAllTasksForUser(int userId)
//     {
//         var taskToRemove = dbContext.TaskItem.Where(t => t.UserId == userId);
//         if (!taskToRemove.Any())
//         {
//             return;
//         }
//         dbContext.TaskItem.RemoveRange(taskToRemove);
//         await dbContext.SaveChangesAsync();
//     }
//     
//     public async Task DeleteAllTasks()
//     {
//         var taskToRemove = dbContext.TaskItem.Where(x => true);   
//         if (!taskToRemove.Any())
//         {
//             return;
//         }
//         dbContext.TaskItem.RemoveRange(taskToRemove);
//         await dbContext.SaveChangesAsync();
//     }
// }