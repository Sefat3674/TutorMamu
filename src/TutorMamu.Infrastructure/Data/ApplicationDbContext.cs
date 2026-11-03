using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutorMamu.Domain.Entities;

namespace TutorMamu.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TutorProfile> TutorProfiles => Set<TutorProfile>();
    public DbSet<TutorTopic> TutorTopics => Set<TutorTopic>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Topic>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Parent).WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TutorProfile>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.AverageRating).HasPrecision(3, 2);
            e.Property(x => x.Headline).HasMaxLength(200);
            e.Property(x => x.Bio).HasMaxLength(2000);
            e.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        });

        builder.Entity<TutorTopic>(e =>
        {
            e.HasIndex(x => new { x.TutorProfileId, x.TopicId }).IsUnique();
            e.HasOne(x => x.TutorProfile).WithMany(x => x.TutorTopics)
                .HasForeignKey(x => x.TutorProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Topic).WithMany(x => x.TutorTopics)
                .HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Booking>(e =>
        {
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.CommissionAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.TutorPayout);
            e.HasIndex(x => new { x.TutorProfileId, x.StartTime });
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TutorProfile).WithMany().HasForeignKey(x => x.TutorProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Booking).WithMany(x => x.Payments)
                .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Review>(e =>
        {
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationSetting>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
        });
    }
}
