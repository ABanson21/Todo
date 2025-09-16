using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoBackend.Model;
using TodoBackend.Services;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class UserController(UserService repository, ILogger<TasksController> logger): ControllerBase
{
    [Authorize()]
    [HttpGet]
    [Route("claims")]
    public async Task<IActionResult> GetAuthenticatedUserInfo()
    { 
        var username = User.Identity?.Name;
        
        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        
        return Ok(new { Username = username, Roles = roles });
    }
    
    [Authorize(Roles = "User,Admin")]
    [HttpGet]
    [Route("admin")]
    public async Task<IActionResult> GetAdminInfo()
    { 
        return Ok("Welcome to admin area. Tread carefully!. The power is strong with this one.");
    }
    
    
    [HttpGet]
    [Route("all")]
    public async Task<IActionResult> GetAllUsers()
    { 
        logger.LogInformation("Getting all users");
        var tasks = await repository.GetAll();
        return Ok(tasks);
    }

    [HttpPut]
    [Route("create")]
    public async Task<IActionResult> Adduser([FromBody] User user)
    {
        if (!ModelState.IsValid) return BadRequest();
        await repository.Create(user);
        return Ok($"User created successfully");
    }
    
    
}