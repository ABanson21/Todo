namespace TodoBackend.Model;

public class Video
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int UploaderId { get; set; }
    public User Uploader { get; set; }
    public DateTime CreatedAt { get; set; } 
}