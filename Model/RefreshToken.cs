namespace TodoBackend.Model;

public class RefreshToken
{
    public int Id { get; set; }                  
    public int UserId { get; set; }    
    public User User { get; set; }
    public string Token { get; set; }            
    public DateTime Expires { get; set; }        
    public DateTime Created { get; set; }        
    public string CreatedByIp { get; set; }      
    public DateTime? Revoked { get; set; }       
    public string RevokedByIp { get; set; }      
    public string ReplacedByToken { get; set; }  

    // ✅ Helper property (not stored in DB unless you want)
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive => Revoked == null && !IsExpired;
}

