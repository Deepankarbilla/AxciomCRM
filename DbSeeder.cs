using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, bool seedDemo)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();

        foreach (var r in Roles.All)
            if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

        if (!seedDemo) return;   // demo accounts only in Development

        var admin = await EnsureUser(um, "admin@acxiom.com", "System Admin", "Admin@123", Roles.Admin);
        var mgr = await EnsureUser(um, "manager@acxiom.com", "Sales Manager", "Manager@123", Roles.Manager);
        var sales = await EnsureUser(um, "sales@acxiom.com", "Sales Executive", "Sales@123", Roles.SalesExecutive);

        if (await db.Customers.AnyAsync()) return;
        var c1 = new Customer { CustomerCode = Ids.New("CUS"), CustomerName = "Ravi Kumar", Email = "ravi@example.com", Phone = "9876543210", CompanyName = "Kumar Traders", City = "Vijayawada", State = "Andhra Pradesh", CreatedBy = admin.Id, AssignedTo = sales.Id };
        var c2 = new Customer { CustomerCode = Ids.New("CUS"), CustomerName = "Anita Rao", Email = "anita@example.com", Phone = "9876543211", CompanyName = "Rao Textiles", City = "Guntur", State = "Andhra Pradesh", CreatedBy = admin.Id, AssignedTo = sales.Id };
        db.Customers.AddRange(c1, c2);
        db.Leads.AddRange(
            new Lead { LeadCode = Ids.New("LD"), LeadName = "Suresh Babu", Email = "suresh@example.com", Phone = "9876543212", CompanyName = "SB Foods", Source = "Referral", Status = "New", ExpectedValue = 50000, AssignedTo = sales.Id },
            new Lead { LeadCode = Ids.New("LD"), LeadName = "Meena Devi", Email = "meena@example.com", Phone = "9876543213", CompanyName = "Meena Boutique", Source = "Website", Status = "Qualified", ExpectedValue = 120000, AssignedTo = sales.Id },
            new Lead { LeadCode = Ids.New("LD"), LeadName = "Karthik R", Email = "karthik@example.com", Phone = "9876543214", Source = "Event", Status = "Contacted", ExpectedValue = 80000, AssignedTo = mgr.Id });
        await db.SaveChangesAsync();
        db.Opportunities.AddRange(
            new Opportunity { OpportunityName = "Kumar Traders - ERP", CustomerId = c1.CustomerId, Amount = 250000, Stage = "Proposal", Probability = 50, ExpectedCloseDate = DateTime.Today.AddDays(20), AssignedTo = sales.Id },
            new Opportunity { OpportunityName = "Rao Textiles - CRM", CustomerId = c2.CustomerId, Amount = 90000, Stage = "Won", Status = "Won", Probability = 100, ExpectedCloseDate = DateTime.Today.AddDays(-10), AssignedTo = sales.Id });
        db.FollowUps.Add(new FollowUp { CustomerId = c1.CustomerId, FollowUpDate = DateTime.Today.AddDays(2), FollowUpType = "Call", Remarks = "Discuss proposal", AssignedTo = sales.Id });
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUser(UserManager<ApplicationUser> um, string email, string name, string pwd, string role)
    {
        var u = await um.FindByEmailAsync(email);
        if (u != null) return u;
        u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
        var r = await um.CreateAsync(u, pwd);
        if (!r.Succeeded) throw new Exception("Seed failed: " + string.Join("; ", r.Errors.Select(e => e.Description)));
        await um.AddToRoleAsync(u, role);
        return u;
    }
}
