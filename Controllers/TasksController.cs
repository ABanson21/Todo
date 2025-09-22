using Microsoft.AspNetCore.Mvc;
using TodoBackend.Model;
using TodoBackend.Repository;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class TasksController(TaskRepository repository, ILogger<TasksController> logger): ControllerBase
{
    [HttpGet]
    [Route("all")]
    public async Task<IActionResult> GetAllTasks()
    {
        logger.LogInformation("Getting all tasks");
        var tasks = await repository.GetAll();
        return Ok(tasks);
    }

    [HttpGet]
    [Route("getUserTasks/")]
    public async Task<IActionResult> GetTasksForUser([FromBody] string userId)
    {
        logger.LogInformation("Getting tasks for user: {userId}", userId);
        if (string.IsNullOrEmpty(userId)) return BadRequest();
        var resultTasks = await repository.GetTasksForUsers(userId);
        return Ok(resultTasks);
    }

    [HttpPut]
    [Route("addTask")]
    public async Task<IActionResult> AddTask([FromBody] TaskItem task)
    {
        if (!ModelState.IsValid) return BadRequest();
        task.IsCompleted = false;
        task.ModifiedDate = DateTime.UtcNow;
        await repository.Create(task);
        return Ok($"Task created successfully for user");
    }
}