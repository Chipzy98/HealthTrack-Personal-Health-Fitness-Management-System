using HealthTrack.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthTrack.Data
{
    /// <summary>
    /// Entity Framework Core database context. Configures relationships and
    /// transparently encrypts sensitive health columns using the ASP.NET Core Data Protection API.
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly IDataProtector _protector;

        public AppDbContext(DbContextOptions<AppDbContext> options, IDataProtectionProvider dataProtectionProvider)
            : base(options)
        {
            _protector = dataProtectionProvider.CreateProtector("HealthTrack.SensitiveHealthData.v1");
        }

        public DbSet<User> Users { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
        public DbSet<DietPlan> DietPlans { get; set; }
        public DbSet<DietPlanItem> DietPlanItems { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<HealthMetric> HealthMetrics { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Users ----------
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Trainer)
                .WithMany(t => t.Clients)
                .HasForeignKey(u => u.TrainerId);

            // ---------- Entities with two links to User (client + trainer) ----------
            modelBuilder.Entity<Appointment>().HasOne(a => a.Client).WithMany().HasForeignKey(a => a.ClientId);
            modelBuilder.Entity<Appointment>().HasOne(a => a.Trainer).WithMany().HasForeignKey(a => a.TrainerId);
            modelBuilder.Entity<Appointment>().HasIndex(a => new { a.TrainerId, a.ScheduledAt });

            modelBuilder.Entity<WorkoutPlan>().HasOne(p => p.Client).WithMany().HasForeignKey(p => p.ClientId);
            modelBuilder.Entity<WorkoutPlan>().HasOne(p => p.Trainer).WithMany().HasForeignKey(p => p.TrainerId);

            modelBuilder.Entity<DietPlan>().HasOne(p => p.Client).WithMany().HasForeignKey(p => p.ClientId);
            modelBuilder.Entity<DietPlan>().HasOne(p => p.Trainer).WithMany().HasForeignKey(p => p.TrainerId);

            modelBuilder.Entity<Feedback>().HasOne(f => f.Client).WithMany().HasForeignKey(f => f.ClientId);
            modelBuilder.Entity<Feedback>().HasOne(f => f.Trainer).WithMany().HasForeignKey(f => f.TrainerId);

            // ---------- Plan children are deleted with their plan ----------
            modelBuilder.Entity<WorkoutExercise>()
                .HasOne(e => e.WorkoutPlan).WithMany(p => p.Exercises)
                .HasForeignKey(e => e.WorkoutPlanId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DietPlanItem>()
                .HasOne(i => i.DietPlan).WithMany(p => p.Items)
                .HasForeignKey(i => i.DietPlanId).OnDelete(DeleteBehavior.Cascade);

            // Deleting a plan keeps the client's history, it just unlinks it.
            modelBuilder.Entity<WorkoutLog>()
                .HasOne(w => w.WorkoutPlan).WithMany()
                .HasForeignKey(w => w.WorkoutPlanId).OnDelete(DeleteBehavior.SetNull);

            // ---------- Indexes used by dashboards and reports ----------
            modelBuilder.Entity<WorkoutLog>().HasIndex(w => new { w.ClientId, w.Date });
            modelBuilder.Entity<MealLog>().HasIndex(m => new { m.ClientId, m.LoggedAt });
            modelBuilder.Entity<HealthMetric>().HasIndex(m => new { m.ClientId, m.RecordedAt });
            modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });
            modelBuilder.Entity<ActivityLog>().HasIndex(a => a.Timestamp);

            // Users are never hard-deleted (they are deactivated), so every relationship
            // pointing at User is RESTRICT. This also avoids SQL Server "multiple cascade path" errors.
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(e => e.GetForeignKeys())
                         .Where(fk => fk.PrincipalEntityType.ClrType == typeof(User)))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // ---------- Encryption at rest for sensitive health data ----------
            var protector = _protector;
            var encryptedConverter = new ValueConverter<string, string>(
                plain => protector.Protect(plain),
                cipher => protector.Unprotect(cipher));

            modelBuilder.Entity<User>().Property(u => u.MedicalConditions).HasConversion(encryptedConverter);
            modelBuilder.Entity<HealthMetric>().Property(m => m.Notes).HasConversion(encryptedConverter);
        }
    }
}
