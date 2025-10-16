using Microsoft.EntityFrameworkCore;
using TodoBackend.Model;
using TodoBackend.Model.Junctions;

namespace TodoBackend.Database;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> dbContextOptions): DbContext(dbContextOptions)
{
    public DbSet<RefreshToken> RefreshToken { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<StudentProfile> Student { get; set; }
    public DbSet<Belt> Belt { get; set; }
    public DbSet<ParentStudent> ParentStudent { get; set; }
    public DbSet<StudentInstructor> StudentInstructor { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // set primary keys and relationships here 
        SetupRefreshTokenEntity(modelBuilder);
        SetupUserEntity(modelBuilder);
        SetupStudentEntity(modelBuilder);
        SetupBeltEntity(modelBuilder);
        SetupParentStudentJunction(modelBuilder);
        SetupStudentInstructorJunction(modelBuilder);
    } 
    
    private void SetupUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user"); 

            entity.HasKey(e => e.Id);
            
            // Add unique indexes for UserName and Email
            entity.HasIndex(e => e.UserName).IsUnique();
            
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired(); 
            entity.Property(e => e.Role).IsRequired().HasMaxLength(255);
        });
    }
    
    private void SetupRefreshTokenEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refreshtoken"); 

            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.Token).IsUnique();
            
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Expires).IsRequired();
            entity.Property(e => e.Created).IsRequired();
            entity.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(45);
            entity.Property(e => e.Revoked).IsRequired(false);
            entity.Property(e => e.RevokedByIp).IsRequired(false).HasMaxLength(45);
            entity.Property(e => e.ReplacedByToken).IsRequired(false).HasMaxLength(255);
            entity.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private void SetupStudentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("student"); 
            entity.HasKey(e => e.UserId);
            
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DateOfBirth).IsRequired();
            entity.Property(e => e.BeltId).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.Notes).IsRequired(false).HasMaxLength(1000);
            entity.HasOne(x => x.User)
                .WithOne(x => x.StudentProfile)
                .HasForeignKey<StudentProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Belt)
                .WithMany(b => b.StudentProfiles)
                .HasForeignKey(e => e.BeltId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
    
private void SetupBeltEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Belt>(entity =>
        {
            entity.ToTable("belt");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Rank).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(100);
            

        });
    }

    private void SetupParentStudentJunction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParentStudent>(entity =>
            {
                entity.ToTable("parent_student");
                entity.HasKey(e => new { e.StudentId, e.ParentId });
                entity.HasOne(e => e.Student)
                    .WithMany(e => e.ParentStudents)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.ParentStudents)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
    }
    
    private void SetupStudentInstructorJunction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentInstructor>(entity =>
        {
            entity.ToTable("student_instructor");
            entity.HasKey(e => new { e.StudentId, e.InstructorId });
            entity.HasOne(e => e.Student)
                .WithMany(e => e.StudentInstructors)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Instructor)
                .WithMany(e => e.StudentInstructors)
                .HasForeignKey(e => e.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}