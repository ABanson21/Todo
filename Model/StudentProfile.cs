using TodoBackend.Model.Junctions;

namespace TodoBackend.Model;

public class StudentProfile
{
    public int UserId { get; set; }
    public User User { get; set; }
    
    public DateOnly DateOfBirth { get; set; }
    public DateOnly StartDate { get; set; }
    public string? Notes { get; set; }
    
    public int BeltId { get; set; }
    public Belt Belt { get; set; }
    
    // navigation properties
    public ICollection<StudentInstructor> StudentInstructors { get; set; } = new List<StudentInstructor>();
    public ICollection<ParentStudent> ParentStudents { get; set; } = new List<ParentStudent>();
}