namespace TodoBackend.Model;

public class Belt
{
    public int Id { get; set; }
    public string Color { get; set; }
    public int Rank { get; set; }
    
    // navigation properties
    public ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();

}