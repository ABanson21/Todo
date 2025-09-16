using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using TodoBackend.Configurations;

namespace TodoBackend.Database;

public class DatabaseContext<T>(IOptions<DatabaseConfig> databaseConfigs) : IDatabaseContext<T> where T : class, new()
{
    private readonly string _connectionString = "Server=" + databaseConfigs.Value.Server
                                                          + ";Port=" + databaseConfigs.Value.Port
                                                          + ";Database=" + databaseConfigs.Value.Database
                                                          + ";User=" + databaseConfigs.Value.User
                                                          + ";Password=" + databaseConfigs.Value.Password + ";";

    public async Task ExecuteAction(string query, Dictionary<string, object>? parameters = null)
    {
        var connection = new MySqlConnection(_connectionString);
        var mySqlCommand = new MySqlCommand();
        try
        {
            Console.WriteLine("Connecting to MySQL...");
            await connection.OpenAsync();
            mySqlCommand.Connection = connection;
            mySqlCommand.CommandText = query;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    mySqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            mySqlCommand.ExecuteNonQuery();
            Console.WriteLine("Action On Database Executed Successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            await connection.CloseAsync();
            Console.WriteLine("Connection Closed");
        }
    }
    
    public async Task<List<T>> GetResultList(string query, Dictionary<string, object>? parameters = null)
    {
        var models = new List<T>();
        var connection = new MySqlConnection(_connectionString);
        var mySqlCommand = new MySqlCommand();
        try
        {
            Console.WriteLine("Connecting to MySQL...");
            await connection.OpenAsync();
            mySqlCommand.Connection = connection;
            mySqlCommand.CommandText = query;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    mySqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            var reader = await mySqlCommand.ExecuteReaderAsync();
            while (reader.Read())
            {
                var model = GetModelFromReader<T>(reader);
                models.Add(model);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
        finally
        {
            await connection.CloseAsync();
            Console.WriteLine("Connection Closed");
        }
        return models;
    }
    
    public async Task<T> GetResult(string query, Dictionary<string, object>? parameters = null)
    {
        var connection = new MySqlConnection(_connectionString);
        var mySqlCommand = new MySqlCommand();
        try
        {
            Console.WriteLine("Connecting to MySQL...");
            await connection.OpenAsync();
            mySqlCommand.Connection = connection;
            mySqlCommand.CommandText = query;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    mySqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            var reader = await mySqlCommand.ExecuteReaderAsync();
            if (reader.Read())
            {
                var model = GetModelFromReader<T>(reader);
                return model;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
        finally
        {
            await connection.CloseAsync();
            Console.WriteLine("Connection Closed");
        }

        return default!;
    }
    
    public async Task<TResult> GetScalarResult<TResult>(string query, Dictionary<string, object>? parameters = null)
    {
        var connection = new MySqlConnection(_connectionString);
        var mySqlCommand = new MySqlCommand();
        try
        {
            Console.WriteLine("Connecting to MySQL...");
            await connection.OpenAsync();
            mySqlCommand.Connection = connection;
            mySqlCommand.CommandText = query;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    mySqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            var reader = await mySqlCommand.ExecuteScalarAsync();
            if (reader != null)
            {
                return (TResult)Convert.ChangeType(reader, typeof(TResult));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
        finally
        {
            await connection.CloseAsync();
            Console.WriteLine("Connection Closed");
        }

        return default!;
    }

    
    private T GetModelFromReader<T>(MySqlDataReader reader) where T : new()
    {
        var model = new T();
        var fieldInfoList = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var field in fieldInfoList)
        {
            if (field.Name == "Email") continue;
            if (reader.IsDBNull(reader.GetOrdinal(field.Name))) continue;
            var value = reader[field.Name];
            field.SetValue(model, field.PropertyType switch
            {
                var t when t == typeof(string) => value.ToString(),
                var t when t == typeof(int) => Convert.ToInt32(value),
                var t when t == typeof(DateTime) => Convert.ToDateTime(value),
                var t when t == typeof(bool) => Convert.ToBoolean(value),
                var t => Convert.ChangeType(value, t)
            } );
        }
        return model;
    }
}