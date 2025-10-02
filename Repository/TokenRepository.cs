using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Repository;

public class TokenRepository(AppDatabaseContext databaseContext, ILogger<TokenRepository> logger)
{

    private static RefreshToken CreateNewRefreshToken(int userId, string tokenString, string ipAddress)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = tokenString,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            Revoked = null,
            RevokedByIp = null,
            ReplacedByToken = null
        };
    }

    public async Task<(int UserId, string NewRefreshToken)> RefreshUserToken(string oldRefreshToken, string ipAddress)
    {
        logger.LogInformation("Executing transactions for token refresh");

        await using var transaction = await databaseContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var token = await databaseContext.RefreshToken
                .FromSqlRaw("SELECT * FROM RefreshToken WHERE Token = {0} FOR UPDATE", oldRefreshToken)
                .FirstOrDefaultAsync();

            if (token is null)
            {
                throw new InvalidOperationException("Token was modified concurrently or doesnt exist. Please try again.");
            }

            if (token.Revoked != null)
            {
                await RevokeAllTokens(token.UserId, false);
                await databaseContext.SaveChangesAsync();
                await transaction.CommitAsync(); 
                
                throw new InvalidOperationException(
                    "Token reuse detected. Transaction flagged. User tokens will be reset. Please login again on all devices.");
            }

            if (token.Expires <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Refresh token expired. Please login again.");
            }

            var newTokenString = GenerateTokenString();
            var newRefreshToken = CreateNewRefreshToken(token.UserId, newTokenString, ipAddress);

            // Revoke old token
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReplacedByToken = newTokenString;

            await databaseContext.RefreshToken.AddAsync(newRefreshToken);

            await databaseContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return (token.UserId, newTokenString);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh, rolling back transaction.");
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<bool> RevokeToken(string tokenToRevoke, string ipAddress)
    {
        logger.LogInformation("Executing transactions for token revoke");
        await using var transaction = await databaseContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var token = await databaseContext.RefreshToken
                .FromSqlRaw("SELECT * FROM RefreshToken WHERE Token = {0} FOR UPDATE", tokenToRevoke)
                .FirstOrDefaultAsync();

            if (token is null)
            {
                throw new InvalidOperationException("Token invalid. Please try again.");
            }

            if (token.Revoked is not null)
            {
                throw new InvalidOperationException("Token already revoked");
            }

            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            var recordsUpdated = await databaseContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return recordsUpdated > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token revoke, rolling back transaction.");
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    
    public async Task<int> RevokeAllTokens(int userId, bool saveChanges = true)
    {
        var tokensRevoked = await databaseContext.RefreshToken
            .Where(x => x.UserId == userId && x.Revoked == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Revoked, DateTime.UtcNow));
        
        if(saveChanges)
            await databaseContext.SaveChangesAsync();
        
        return tokensRevoked;
    }
    
    
    public async Task CreateToken(RefreshToken refreshToken)
    {
        await databaseContext.RefreshToken.AddAsync(refreshToken);
        await databaseContext.SaveChangesAsync();
    }
    
    public async Task<List<RefreshToken>> GetAllTokens()
    {
        return await databaseContext.RefreshToken.ToListAsync();
    }
    
    public async Task<RefreshToken?> GetToken(string token)
    {
        return await databaseContext.RefreshToken.FirstOrDefaultAsync(x => x.Token == token);
    }
    
    public async Task DeleteToken(string token)
    {
        var tokenToRemove = databaseContext.RefreshToken.FirstOrDefault(t => t.Token == token); 
        if (tokenToRemove == null)
        {
            throw new ArgumentException("Task not found");
        }
        databaseContext.RefreshToken.Remove(tokenToRemove);
        await databaseContext.SaveChangesAsync();
    }
    
    public async Task DeleteAllTokensForUser(int userId)
    {
        var tokenToRemove = databaseContext.RefreshToken.Where(t => t.UserId == userId);
        if (!tokenToRemove.Any())
        {
            return;
        }
        databaseContext.RefreshToken.RemoveRange(tokenToRemove);
        await databaseContext.SaveChangesAsync();
    }
    
    public async Task DeleteAllTokens()
    {
        var tokensToRemove = databaseContext.RefreshToken.Where(x => true);   
        if (!tokensToRemove.Any())
        {
            return;
        }
        databaseContext.RefreshToken.RemoveRange(tokensToRemove);
        await databaseContext.SaveChangesAsync();
    }
    
    
    public string GenerateTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}