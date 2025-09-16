using TodoBackend.Database;

namespace TodoBackend.Services;

public abstract class BaseRepository<T> where T : class, new()
{
    private readonly IDatabaseContext<T> _dbContext;

    protected BaseRepository(IDatabaseContext<T> dbContext)
    {
        _dbContext = dbContext;
        CreateTable().GetAwaiter().GetResult();
    }
    
    private async Task CreateTable()
    {
        var properties = typeof(T).GetProperties();
        var tableName = typeof(T).Name;
        
        var columns = properties.Select(p => 
        {
            var isIdField = p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
            var isUserId = p.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase);
            if (isIdField)
            {
                return $"{p.Name} INT PRIMARY KEY AUTO_INCREMENT";
            }
            
            return isUserId ?
                $"FOREIGN KEY ({p.Name}) REFERENCES User(Id)" : 
                $"{p.Name} {MapToSqlObject(p.PropertyType)}";
        });

        var sqlQuery = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
        
        await _dbContext.ExecuteAction(sqlQuery);
    }
    
    public async Task DropTable()
    {
        var tableName = typeof(T).Name;
        var sql = $"DROP TABLE IF EXISTS {tableName}";

        await _dbContext.ExecuteAction(sql);
    }
    
    public async Task<List<T>> GetAll()
    {
        var tableName = typeof(T).Name;
        var query = $"SELECT * FROM {tableName}";

        return await _dbContext.GetResultList(query);
    }
    
    private async Task<T> GetById(int id)
    {
        var tableName = typeof(T).Name;
        var query = $"SELECT * FROM {tableName} WHERE Id = @Id";
        
        var parameters = new Dictionary<string, object> { { "@Id", id } };

        return await _dbContext.GetResult(query, parameters);
    }
    
    public async Task DeleteById(int id)
    {
        var tableName = typeof(T).Name;
        var query = $"DELETE FROM {tableName} WHERE Id = @Id";
        
        var parameters = new Dictionary<string, object> { { "@Id", id } };

        await _dbContext.ExecuteAction(query, parameters);
    }
    
    public async Task DeleteAll()
    {
        var tableName = typeof(T).Name;
        var sql = $"DELETE FROM {tableName}";

        await _dbContext.ExecuteAction(sql);
    }
    
    public async Task<T> Create(T item)
    {
        var tableName = typeof(T).Name;
        var properties = typeof(T).GetProperties().Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
        
        var columnNames = string.Join(", ", properties.Select(p => p.Name));
        var paramNames = string.Join(", ", properties.Select(p => "@" + p.Name));
        
        var query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames})";
        
        var parameters = properties.ToDictionary(p => "@" + p.Name, p => p.GetValue(item) ?? DBNull.Value);

        await _dbContext.ExecuteAction(query, parameters);
        
        return item;
    }
    
    public async Task<T> Update(T item)
    {
        var tableName = typeof(T).Name;
        var properties = typeof(T).GetProperties().Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
        
        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
        
        var query = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id";
        
        var parameters = properties.ToDictionary(p => "@" + p.Name, p => p.GetValue(item) ?? DBNull.Value);
        
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            parameters.Add("@Id", idProperty.GetValue(item) ?? DBNull.Value);
        }

        await _dbContext.ExecuteAction(query, parameters);

        return item;
    }
    
    private static string MapToSqlObject(Type type)
    {
        return type.Name.ToLower() switch
        {
            "int32" => "INT",
            "string" => "VARCHAR(255)",
            "datetime" => "DATETIME",
            "boolean" => "BOOLEAN",
            "decimal" => "DECIMAL(18,2)",
            "double" => "DOUBLE",
            "guid" => "CHAR(36)",
            _ => "VARCHAR(255)"
        };
    }
}