using System.Security.Claims; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TodoBackend.Configurations;
using TodoBackend.Model;
using TodoBackend.Model.Auth;
using TodoBackend.Services;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class AuthController(ILogger<AuthController> logger, AuthProvider authProvider, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        return await RegisterUser(request, "User");
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest request)
    {
        return await RegisterUser(request, "Admin");
    }
    
    [HttpPost]
    [EnableRateLimiting("LoginPolicy")]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password must be provided.");
            }
            
            var result = await authProvider.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString()!);
            return Ok(new { accessToken = result.accessToken, refreshToken = result.refreshToken });

        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during login. Exception: {Message}", ex.Message);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
    
    [HttpPost]
    [EnableRateLimiting("RefreshPolicy")]
    [Route("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken) || string.IsNullOrWhiteSpace(request.IpAddress))
            {
                return BadRequest("refreshToken and ipAddress must be provided.");
            }
            
            var result = await authProvider.RefreshTokenAsync(request.RefreshToken, request.IpAddress);
            return Ok(new { accessToken = result.accessToken, refreshToken = result.refreshToken });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex) when (ex.Message.Contains("NpgsqlTransaction has completed"))
        {
            return StatusCode(500, "Request Revoked. Please login on all devices again.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during token refresh. Exception: {Message}", ex.Message);
            return StatusCode(500, ex.Message);
        }
    }
    
    [HttpPost("logout")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        try
        {
            var revokeSucceeded = await authProvider.RevokeTokenAsync(request.RefreshToken, request.IpAddress);
            if (!revokeSucceeded)
                return BadRequest("Token revocation failed. Token may be invalid or already revoked.");

            return Ok("Logged out.");
        }
        catch (InvalidOperationException ex)
        {
            return Ok("Logged out.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during token revoke. Exception: {Message}", ex.Message);
            return StatusCode(500, ex.Message);
        }
      
    }

    [HttpPost("logout-all")]
    [Authorize] 
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            var userContained = Int32.TryParse(User.FindFirstValue(nameof(AppConstants.UserId)), out var userId);

            if (!userContained)
            {
                return BadRequest("UserId claim is missing or invalid.");
            }
        
            var count = await authProvider.RevokeAllTokenAsync(userId);

            return Ok($"{count} sessions revoked");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during token revoke. Exception: {Message}", ex.Message);
            return StatusCode(500, ex.Message);
        }
        
    }
    
    private async Task<IActionResult> RegisterUser(RegisterRequest request, string role)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request.");
        }

        try
        {
            await authProvider.RegisterUserAsync(request, role);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during user registration. Exception: {Message}", ex.Message);
            return StatusCode(500, ex.Message);
        }
        
        return Ok("User created successfully");
    }
    
}