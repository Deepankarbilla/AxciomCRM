using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers;

public class CustomersController(ApplicationDbContext db, IAuditService audit, ValidationService rules) : CrmController<Customer>(db, audit, rules)
{
    protected override string EntityName => "Customer";
    protected override string KeyName => nameof(Customer.CustomerId);

    protected override IQueryable<Customer> Filter(IQueryable<Customer> q, string? s, string? status, DateTime? d)
    {
        if (!string.IsNullOrEmpty(s))
            q = q.Where(c => c.CustomerName.Contains(s) || c.Email.Contains(s) || c.Phone.Contains(s) || (c.CompanyName != null && c.CompanyName.Contains(s)));
        if (!string.IsNullOrEmpty(status)) q = q.Where(c => c.Status == status);
        return q;
    }
    protected override void Normalize(Customer c)
    {
        c.CustomerName = (c.CustomerName ?? "").Trim(); c.Email = (c.Email ?? "").Trim(); c.Phone = (c.Phone ?? "").Trim();
    }
    protected override void OnCreate(Customer c) { c.CustomerCode = Ids.New("CUS"); c.CreatedDate = DateTime.Now; c.CreatedBy = CurrentUserId; }
    protected override void KeepServerFields(Customer m, Customer e) { m.CustomerCode = e.CustomerCode; m.CreatedDate = e.CreatedDate; m.CreatedBy = e.CreatedBy; }
    protected override Task<List<RuleError>> BusinessErrors(Customer m, Customer? e) => Rules.CustomerAsync(m);
    protected override Task AddLookups()
    {
        ViewBag.StatusList = new SelectList(Lists.CustomerStatus);
        ViewBag.FilterList = ViewBag.StatusList;
        return Task.CompletedTask;
    }
    protected override Task PerformDelete(Customer c) { c.Status = "Inactive"; return Task.CompletedTask; }   // soft delete / deactivate
}
