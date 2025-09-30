using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoBackend.Repository;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class UserController(UserRepository repository, ILogger<TasksController> logger): ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    { 
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        
        return Ok(new { Username = username, Role = role });
    }
    
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminData()
    { 
        return Ok("This is protected Admin-only data!");
    }
    
    [Authorize(Roles = "User")]
    [HttpGet("user")]
    public async Task<IActionResult> GetUserData()
    { 
        return Ok("This is protected User-only data!");
    }
}