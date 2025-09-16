namespace TodoBackend.Database;

public interface IDatabaseContext<T> where T : class, new()
{
    Task<T> GetResult(string query, Dictionary<string, object>? parameters = null);
    Task<List<T>> GetResultList(string query, Dictionary<string, object>? parameters = null);
    Task ExecuteAction(string query, Dictionary<string, object>? parameters = null);
    Task<TResult> GetScalarResult<TResult>(string query, Dictionary<string, object>? parameters = null);
    
}