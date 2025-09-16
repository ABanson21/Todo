namespace TodoBackend.Model;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Role { get; set; }
    public string Email =>  UserName;

}