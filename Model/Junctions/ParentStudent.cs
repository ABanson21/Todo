namespace TodoBackend.Model.Junctions;

public class ParentStudent
{
    public int StudentId { get; set; }
    public int ParentId { get; set; }
    
    public StudentProfile Student { get; set; }
    public User Parent { get; set; }
}