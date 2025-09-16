using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoBackend.Configurations;
using TodoBackend.Model;
using TodoBackend.Model.Auth;
using TodoBackend.Services;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class AuthController(ILogger<AuthController> logger, UserService repository, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    
    
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request.");
        }
        
        var returnedCount = await repository.CheckUserExistence(request.Username);
        
        if(returnedCount > 0)
            return BadRequest("Username exists with another account. Please choose another username or login.");
        
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.Username,
            PasswordHash = passwordHash,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Role = "User",
        };

        await repository.Create(newUser);
        
        return Ok("User created successfully");
    }
    

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password must be provided.");
        }
        
        var returnedUser = await repository.UserLogin(request.Username, request.Password);

        if (returnedUser == null)
        {
            return Unauthorized("Invalid Credentials");
        }
        
        // Verify password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, returnedUser.PasswordHash))
            return Unauthorized("Invalid credentials");
        
        var claimsList = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, returnedUser.Role)
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claimsList,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials);
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        return Ok(tokenString);
    }
    
}