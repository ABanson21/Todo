namespace TodoBackend.Model.Auth;

public record RefreshRequest(string RefreshToken, string IpAddress)
{
    
}
