using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using MySqlConnector;
using TodoBackend.Database;
using TodoBackend.Model;

namespace TodoBackend.Repository;

public class TokenRepository(IDatabaseContext<RefreshToken> dbContext, ILogger<TokenRepository> logger)
    : BaseRepository<RefreshToken>(dbContext)
{
    protected override Task CreateTable()
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS RefreshToken (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT,
                Token VARCHAR(255),
                Expires DATETIME,
                Created DATETIME,
                CreatedByIp VARCHAR(45),
                Revoked DATETIME NULL,
                RevokedByIp VARCHAR(45) NULL,
                ReplacedByToken VARCHAR(255) NULL,
                FOREIGN KEY (UserId) REFERENCES User(Id) ON DELETE CASCADE
            )";

        return dbContext.ExecuteAction(createTableSql);
    }

    public async Task<(int UserId, string NewRefreshToken)> RefreshUserToken(string oldRefreshToken, string ipAddress)
    {
        logger.LogInformation("Executing transactions for token refresh");

        var token = await GetToken(oldRefreshToken);
        if (token == null)
        {
            throw new InvalidOperationException("Invalid refresh token");
        }
        
        if (token.Revoked != null)
        {
            await RevokeAllTokens(token.UserId);
            throw new InvalidOperationException(
                "Token reuse detected. Transaction flagged. User tokens will be reset. Please login again on all devices.");
        }
        
        if (token.Expires <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token expired. Please login again.");
        }
        
        return await dbContext.ExecuteTransactions(async sqlCommand =>
        {
            
            sqlCommand.CommandText = $"SELECT {nameof(RefreshToken.Id)} FROM {nameof(RefreshToken)} WHERE {nameof(RefreshToken.Token)} = @Token FOR UPDATE";
            sqlCommand.Parameters.Clear();
            sqlCommand.Parameters.AddWithValue("@Token", oldRefreshToken);
            var lockedTokenId = (int?)await sqlCommand.ExecuteScalarAsync();
            if (lockedTokenId == null)
            {
                throw new InvalidOperationException("Token was modified concurrently. Please try again.");
            }
            
            // Insert new
            var newRefreshToken = GenerateRefreshToken();
            var newExpiryDate = DateTime.UtcNow.AddDays(7);
            var now = DateTime.UtcNow;

            // Revoke old atmically
            sqlCommand.CommandText =
                $@"UPDATE {nameof(RefreshToken)} SET 
                    {nameof(RefreshToken.Revoked)} = @Revoked, 
                    {nameof(RefreshToken.ReplacedByToken)} = @NewToken, 
                    {nameof(RefreshToken.RevokedByIp)} = @RevokedByIp 
                WHERE Token = @OldToken AND {nameof(RefreshToken.Revoked)} IS NULL";
            sqlCommand.Parameters.Clear();
            sqlCommand.Parameters.AddWithValue("@Revoked", now);
            sqlCommand.Parameters.AddWithValue("@RevokedByIp", ipAddress);
            sqlCommand.Parameters.AddWithValue("@OldToken", oldRefreshToken);
            sqlCommand.Parameters.AddWithValue("@NewToken", newRefreshToken);
            
            var affectedRows = await sqlCommand.ExecuteNonQueryAsync();
            if (affectedRows == 0)
            {
                throw new InvalidOperationException("Failed to revoke old refresh token. Please try again.");
            }
            
            sqlCommand.CommandText = $@"INSERT INTO {nameof(RefreshToken)}
                                (UserId, Token, Expires, Created, CreatedByIp, Revoked, RevokedByIp, ReplacedByToken)
                                VALUES (@UserId, @Token, @Expires, @Created, @CreatedByIp, NULL, NULL, NULL)";
            sqlCommand.Parameters.Clear();
            sqlCommand.Parameters.AddWithValue("@UserId", token.UserId);
            sqlCommand.Parameters.AddWithValue("@Token", newRefreshToken);
            sqlCommand.Parameters.AddWithValue("@Expires", newExpiryDate);
            sqlCommand.Parameters.AddWithValue("@Created", now);
            sqlCommand.Parameters.AddWithValue("@CreatedByIp", ipAddress);
            await sqlCommand.ExecuteNonQueryAsync(); 
            
            return (token.UserId, newRefreshToken);
        });
    }

    public async Task<bool> RevokeToken(string oldRefreshToken, string ipAddress)
    {
        return await dbContext.ExecuteTransactions(async sqlCommand =>
        {
            var getTokenQuery = $"SELECT * FROM {nameof(RefreshToken)} WHERE Token = @Token FOR UPDATE";

            sqlCommand.CommandText = getTokenQuery;
            sqlCommand.Parameters.Clear();
            sqlCommand.Parameters.AddWithValue("@Token", oldRefreshToken);

            int? tokenId;
            DateTime? revoked;

            await using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Invalid or expired refresh token");

                tokenId = reader.GetInt32(reader.GetOrdinal(nameof(RefreshToken.Id)));
                revoked = reader.IsDBNull(reader.GetOrdinal(nameof(RefreshToken.Revoked)))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal(nameof(RefreshToken.Revoked)));
            }

            if (tokenId == null)
                throw new InvalidOperationException("Invalid or expired refresh token");

            if (revoked != null)
                throw new InvalidOperationException("Token already revoked");

            var revokeOldTokenCommand =
                $"UPDATE {nameof(RefreshToken)} SET {nameof(RefreshToken.Revoked)} = @Revoked, {nameof(RefreshToken.RevokedByIp)} = @RevokedByIp WHERE Token = @Token AND {nameof(RefreshToken.Revoked)} IS NULL";
            // Revoke old

            sqlCommand.CommandText = revokeOldTokenCommand;
            sqlCommand.Parameters.Clear();
            sqlCommand.Parameters.AddWithValue("@Revoked", DateTime.UtcNow);
            sqlCommand.Parameters.AddWithValue("@RevokedByIp", ipAddress);
            sqlCommand.Parameters.AddWithValue("@Token", oldRefreshToken);
            var changed = await sqlCommand.ExecuteNonQueryAsync();
            return changed > 0;
        });
    }
    
    public async Task<int> RevokeAllTokens(int userId)
    {
        logger.LogWarning("Revoking all tokens for user {UserId}", userId);
        var query =
            $"UPDATE {nameof(RefreshToken)} SET {nameof(RefreshToken.Revoked)} = @Revoked WHERE {nameof(RefreshToken.UserId)} = @UserId AND {nameof(RefreshToken.Revoked)} IS NULL";
        var parameters = new Dictionary<string, object>
        {
            { "@Revoked", DateTime.UtcNow },
            { "@UserId", userId }
        };
        
        return await dbContext.ExecuteAction(query, parameters);
    }

    public async Task<RefreshToken> GetToken(string token)
    {
        const string query = $"SELECT * FROM {nameof(RefreshToken)} WHERE {nameof(RefreshToken.Token)} = @Token";
        var parameters = new Dictionary<string, object>
        {
            { "@Token", token },
        };
        return await dbContext.GetResult(query, parameters);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}