using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;
namespace TaskManager.Api.Data;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkItem> Tasks => Set<WorkItem>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.ToTable("Users"); e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired(); e.HasIndex(x => x.Email).IsUnique();
        });
        model.Entity<WorkItem>(e =>
        {
            e.ToTable("Tasks", table =>
            {
                table.HasTrigger("TR_Tasks_StatusTransition");
                table.UseSqlOutputClause(false);
                table.HasCheckConstraint("CK_Tasks_Status", "[Status] IN ('Pending','InProgress','Done')");
                table.HasCheckConstraint("CK_Tasks_Json", "ISJSON([AdditionalData]) = 1");
            });
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.AdditionalData).HasColumnType("nvarchar(max)").HasDefaultValue("{}");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });
        });
    }
}
