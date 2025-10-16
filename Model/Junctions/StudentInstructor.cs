namespace TodoBackend.Model.Junctions;

public class StudentInstructor
{
    public int StudentId { get; set; }
    public int InstructorId { get; set; }
    
    public StudentProfile Student { get; set; }
    public User Instructor { get; set; }
}  