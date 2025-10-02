using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoBackend.Model;
using TodoBackend.Repository;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class TasksController(TaskRepository repository, ILogger<TasksController> logger): ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("all")]
    public async Task<IActionResult> GetAllTasks()
    {
        logger.LogInformation("Getting all tasks");
        var tasks = await repository.GetAllTasks();
        return Ok(tasks);
    }

    [HttpGet]
    [Route("getUserTasks/")]
    [Authorize]
    public async Task<IActionResult> GetTasksForUser()
    {
        var userId = User.FindFirst(c => c.Type == "UserId")?.Value;
        logger.LogInformation("Getting tasks for user: {userId}", userId);
        if (string.IsNullOrEmpty(userId)) return BadRequest();
        var resultTasks = await repository.GetTasksForUsers(userId);
        return Ok(resultTasks);
    }

    [HttpPut]
    [Route("addTask")]
    [Authorize(Policy = AppConstants.CanEditOwnProfile)]
    public async Task<IActionResult> AddTask([FromBody] TaskItem task)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid task data");
        }

        try
        {
            await repository.CreateTask(task);
            return Ok($"Task created successfully for user");
        }
        catch (Exception ex)
        {
            return StatusCode(500,ex.Message);
        }
    }
    
    
    [HttpPut]
    [Route("editTask")]
    [Authorize(Policy = AppConstants.CanEditOwnProfile)]
    public async Task<IActionResult> UpdateTask([FromBody] TaskItem task)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid task data");
        }

        try
        {
            await repository.UpdateTask(task);
            return Ok($"Task updated successfully for user");
        }
        catch (Exception ex)
        {
            return StatusCode(500,ex.Message);
        }
    }
}