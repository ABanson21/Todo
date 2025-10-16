using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoBackend.Configurations;
using TodoBackend.Model;
using TodoBackend.Model.Auth;
using TodoBackend.Model.Enums;
using TodoBackend.Repository;

namespace TodoBackend.Services;

public class AuthProvider(ILogger<AuthProvider> logger, 
    UserRepository userRepository, 
    TokenRepository tokenRepository,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly ILogger<AuthProvider> _logger = logger;
    
    public async Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        return await tokenRepository.RevokeToken(refreshToken, ipAddress);
    }
    
    public async Task<int> RevokeAllTokenAsync(int userId)
    {
        return await tokenRepository.RevokeAllTokens(userId);
    }
    
    public async Task<(string accessToken, string refreshToken)> LoginAsync(LoginRequest request, string ipAddress)
    {
        var returnedUser = await userRepository.GetUserByUsername(request.Username);
        if (returnedUser == null)
        {
            throw new UnauthorizedAccessException("No user exists for that username and password combination. Please try again.");
        }
        
        // Verify password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, returnedUser.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials. Please check your username and password.");
        }
        
        var accessToken = GenerateJwtAccessToken(request.Username, returnedUser.Role.ToString(), returnedUser.Id.ToString());
        var refreshToken = tokenRepository.GenerateTokenString();
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);
        var token = new RefreshToken()
        {
            UserId = returnedUser.Id,
            Token = refreshToken,
            Expires = refreshTokenExpiration,
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            Revoked = null,
            RevokedByIp = string.Empty,
            ReplacedByToken = string.Empty,
        };
        
        await tokenRepository.CreateToken(token);
        return (accessToken, refreshToken);
    }
    
    
    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken, string ipAddress)
    {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
        var (userId, newRefreshToken) = await tokenRepository.RefreshUserToken(refreshToken, ipAddress);
        var returnedUser = await userRepository.GetUserByUserId(userId);
        if (returnedUser is null)
        {
            throw new UnauthorizedAccessException("No user record exists. Please login again.");
        }
        
        var accessToken = GenerateJwtAccessToken(returnedUser.UserName, returnedUser.Role.ToString(), returnedUser.Id.ToString());
        return (accessToken, newRefreshToken);
    }
    
    public async Task RegisterUserAsync(RegisterRequest request, string role)
    {
        var returnedUser = await userRepository.GetUserByUsername(request.Username);
        
        if(returnedUser != null)
            throw new InvalidOperationException("Username exists with another account. Please choose another username or login.");
        
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
            ProfilePictureUrl = "https://avatar.iran.liara.run/public",
            Role = Enum.TryParse<UserRole>(role, out var result) ? result : UserRole.Student,
        };

        await userRepository.CreateUser(newUser);
    }


    private string GenerateJwtAccessToken(string username, string role, string userId)
    {
        var claimsList = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserId", userId)
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var accessToken = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claimsList,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);
        
       return new JwtSecurityTokenHandler().WriteToken(accessToken);
    }

}