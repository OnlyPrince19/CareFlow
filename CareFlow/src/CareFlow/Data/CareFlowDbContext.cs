using Microsoft.EntityFrameworkCore;
using CareFlow.Models;

namespace CareFlow.Data;

public class CareFlowDbContext : DbContext
{
    public CareFlowDbContext(DbContextOptions<CareFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Patient entity configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.PatientCode).IsRequired().HasMaxLength(20);
            entity.HasIndex(p => p.PatientCode).IsUnique();
            entity.Property(p => p.FullName).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Gender).HasMaxLength(20);
            entity.Property(p => p.ContactNumber).HasMaxLength(30);
        });

        // DiagnosticService entity configuration
        modelBuilder.Entity<DiagnosticService>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(s => s.Code).IsUnique();
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.Department).HasMaxLength(100);
            entity.Property(s => s.DailyCapacity).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(300);
        });

        // Booking entity configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.BookingReference).IsRequired().HasMaxLength(40);
            entity.HasIndex(b => b.BookingReference).IsUnique();
            entity.Property(b => b.BookingDate).IsRequired();
            entity.Property(b => b.CreatedAt).IsRequired();
            entity.Property(b => b.Notes).HasMaxLength(500);

            entity.HasOne(b => b.Patient)
                  .WithMany(p => p.Bookings)
                  .HasForeignKey(b => b.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BookingItem entity configuration
        modelBuilder.Entity<BookingItem>(entity =>
        {
            entity.HasKey(bi => bi.Id);

            // Composite index to prevent the same service from appearing twice in the same booking at database level (BR-06)
            entity.HasIndex(bi => new { bi.BookingId, bi.DiagnosticServiceId }).IsUnique();

            entity.HasOne(bi => bi.Booking)
                  .WithMany(b => b.Items)
                  .HasForeignKey(bi => bi.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bi => bi.DiagnosticService)
                  .WithMany(s => s.BookingItems)
                  .HasForeignKey(bi => bi.DiagnosticServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
