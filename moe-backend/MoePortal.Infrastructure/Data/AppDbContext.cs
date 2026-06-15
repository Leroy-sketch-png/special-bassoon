using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Entities;

namespace MoePortal.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CitizenRecord> CitizenRecords { get; set; } = null!;
    public DbSet<EducationAccount> EducationAccounts { get; set; } = null!;
    public DbSet<ManualAccountAction> ManualAccountActions { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<PaymentAllocation> PaymentAllocations { get; set; } = null!;
    public DbSet<FasApplicationDraft> FasApplicationDrafts { get; set; } = null!;
    public DbSet<EducationAccountTransaction> EducationAccountTransactions { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<CourseFeeComponent> CourseFeeComponents { get; set; } = null!;
    public DbSet<CourseEnrollment> CourseEnrollments { get; set; } = null!;
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; } = null!;
    public DbSet<FasApplication> FasApplications { get; set; } = null!;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();
        var now = DateTimeOffset.UtcNow;
        var user = "System"; // In a real app, this would come from IHttpContextAccessor or similar

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy ??= user;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy ??= user;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = user;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CitizenRecord>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Nric).IsRequired().HasMaxLength(64);
            b.HasIndex(e => e.Nric).IsUnique();
            b.HasOne(e => e.EducationAccount)
             .WithOne(a => a.CitizenRecord)
             .HasForeignKey<EducationAccount>(a => a.CitizenId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EducationAccount>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Balance).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ManualAccountAction>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<EducationAccountTransaction>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            b.HasIndex(e => e.InvoiceNumber).IsUnique();
            b.HasIndex(e => e.WebhookIdempotencyKey).IsUnique();
            b.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            b.Property(e => e.EducationAccountPortion).HasColumnType("decimal(18,2)");
            b.Property(e => e.ExternalPspPortion).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<InvoiceLineItem>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<CourseFeeComponent>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PaymentAllocation>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<FasApplicationDraft>(b =>
        {
            b.HasKey(e => e.Id);
        });
    }
}
