using System.ComponentModel.DataAnnotations.Schema;
using TodoBackend.Model.Enums;
using TodoBackend.Model.Junctions;
using TodoBackend.Security;

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
    public string? ProfilePictureUrl { get; set; }
    public UserRole Role { get; set; }
    [NotMapped]
    public string Email => UserName;
    
    
    // navigation properties
    public StudentProfile? StudentProfile { get; set; } = null!;
    public ICollection<StudentInstructor> StudentInstructors { get; set; } = new List<StudentInstructor>();
    public ICollection<ParentStudent> ParentStudents { get; set; } = new List<ParentStudent>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    

}