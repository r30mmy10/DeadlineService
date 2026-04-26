using DeadlineService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDeliveryHistory> NotificationDeliveryHistories => Set<NotificationDeliveryHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).IsRequired();
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            modelBuilder.Entity<Notification>(entity =>
{
    entity.HasKey(x => x.Id);

    entity.Property(x => x.Message).IsRequired();
    entity.Property(x => x.Channel).IsRequired();
    entity.Property(x => x.DeliveryStatus).IsRequired();

    entity.HasOne(x => x.Task)
        .WithMany()
        .HasForeignKey(x => x.TaskId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<NotificationDeliveryHistory>(entity =>
{
    entity.HasKey(x => x.Id);

    entity.Property(x => x.Status).IsRequired();

    entity.HasOne(x => x.Notification)
        .WithMany()
        .HasForeignKey(x => x.NotificationId)
        .OnDelete(DeleteBehavior.Cascade);
});
        });
    }
}