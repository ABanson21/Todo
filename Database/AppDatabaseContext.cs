using Microsoft.EntityFrameworkCore;
using TodoBackend.Model;

namespace TodoBackend.Database;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> dbContextOptions): DbContext(dbContextOptions)
{
    public DbSet<RefreshToken> RefreshToken { get; set; }
    public DbSet<TaskItem> TaskItem { get; set; }
    public DbSet<User> User { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // set primary keys and relationships here 
        SetupRefreshTokenEntity(modelBuilder);
        SetupTaskItemEntity(modelBuilder);
        SetupUserEntity(modelBuilder);
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
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private void SetupTaskItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("taskitem"); 

            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.UserId, e.IsCompleted });
            entity.HasIndex(e => new { e.UserId, e.DueDate });
            
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.ModifiedDate).IsRequired();
            entity.Property(e => e.IsCompleted).IsRequired();
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    
}