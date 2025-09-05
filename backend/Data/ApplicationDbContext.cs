using Microsoft.EntityFrameworkCore;
using Intellinspect.Backend.Models;

namespace Intellinspect.Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Dataset> Datasets { get; set; }
    public DbSet<TrainingJob> TrainingJobs { get; set; }
    public DbSet<SimulationSession> SimulationSessions { get; set; }
    public DbSet<PredictionResult> PredictionResults { get; set; }
    public DbSet<QualityAlert> QualityAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Dataset configuration
        modelBuilder.Entity<Dataset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.ColumnsJson).HasDefaultValue("[]");
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // TrainingJob configuration
        modelBuilder.Entity<TrainingJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(50);
            entity.Property(e => e.HyperparametersJson).HasDefaultValue("{}");
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Dataset)
                  .WithMany(d => d.TrainingJobs)
                  .HasForeignKey(e => e.DatasetId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SimulationSession configuration
        modelBuilder.Entity<SimulationSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Model)
                  .WithMany(m => m.SimulationSessions)
                  .HasForeignKey(e => e.ModelId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Dataset)
                  .WithMany(d => d.SimulationSessions)
                  .HasForeignKey(e => e.DatasetId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PredictionResult configuration
        modelBuilder.Entity<PredictionResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SensorValuesJson).HasDefaultValue("{}");
            
            entity.HasOne(e => e.SimulationSession)
                  .WithMany(s => s.Predictions)
                  .HasForeignKey(e => e.SimulationSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // QualityAlert configuration
        modelBuilder.Entity<QualityAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Severity).HasConversion<string>();
            entity.Property(e => e.SensorValuesJson).HasDefaultValue("{}");
            
            entity.HasOne(e => e.SimulationSession)
                  .WithMany(s => s.Alerts)
                  .HasForeignKey(e => e.SimulationSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
