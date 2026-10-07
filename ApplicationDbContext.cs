using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Opportunity>().HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FollowUp>().HasOne(f => f.Customer).WithMany().HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<FollowUp>().HasOne(f => f.Lead).WithMany().HasForeignKey(f => f.LeadId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Activity>().HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Activity>().HasOne(a => a.Lead).WithMany().HasForeignKey(a => a.LeadId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<AuditLog>().HasIndex(a => a.CreatedDate);
    }

    // Audit records are append-only
    private void Guard()
    {
        if (ChangeTracker.Entries<AuditLog>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit records are append-only.");
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { Guard(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default) { Guard(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct); }
}
