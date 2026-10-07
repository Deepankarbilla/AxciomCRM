# AcxiomCRM

Role-based CRM built with **ASP.NET Core 8 MVC**, **ASP.NET Core Identity**, **EF Core (SQL Server)**, **Bootstrap 5** and **Chart.js**.

## Run
1. Install .NET 8 SDK and SQL Server / LocalDB.
2. Edit `ConnectionStrings:DefaultConnection` in `appsettings.json` (use `dotnet user-secrets` / env vars for real credentials).
3. `dotnet restore && dotnet run`
   The database is created automatically on first start (or migrated, if you add migrations: `dotnet ef migrations add Initial`).
4. Open the URL shown in the console (Swagger UI at `/swagger` in Development).

### Demo accounts (seeded only in Development)
| Role | Email | Password |
|---|---|---|
| Admin | admin@acxiom.com | Admin@123 |
| Manager | manager@acxiom.com | Manager@123 |
| SalesExecutive | sales@acxiom.com | Sales@123 |

Self-registration (`/Account/Register`) always creates a **SalesExecutive**.

## Structure
- `Models/` entities + constants (statuses, workflow, regexes) · `Data/` DbContext + seeder
- `Services/` `ValidationService` (business rules shared by MVC + API), `AuditService`
- `Controllers/` `CrmController<T>` (generic scoped CRUD + audit), module controllers, `Api/` REST controllers
- `ViewModels/`, `Dtos/`, `Views/`

## Requirement mapping
- **Authentication**: Identity login/register/logout, cookie (HttpOnly, SameSite=Strict, Secure outside dev), password policy (8+, upper/lower/digit/symbol), lockout (5 attempts / 15 min), login rate limiting.
- **Authorization**: `[Authorize(Roles=...)]` + record scoping (SalesExecutive sees only `AssignedTo == self`; enforced server-side on every read/write incl. API). Delete = Admin/Manager. Users/Roles/Audit = Admin.
- **Validation**: DataAnnotations + unobtrusive client validation (required, email, phone `^[6-9]\d{9}$`, length, date, numeric) + custom jQuery rules for close-date / follow-up date; server re-validates everything in `ValidationService`.
- **Business rules**: Amount > 0 (active), 0 ≤ Probability ≤ 100, close date not past (active), follow-up date not before today (Planned), unique customer email/phone + duplicate name/company, lead status workflow (`Lists.LeadFlow`), Convert action (Qualified lead → Customer + Opportunity).
- **Anti-forgery**: `[ValidateAntiForgeryToken]` on all state-changing MVC POSTs. SQL injection: EF Core parameterised queries only.
- **Audit**: login, failed login, lockout, logout, register, create/update/delete, convert, complete, role change, password reset, unlock — with user, old/new JSON, IP. Append-only (DbContext blocks update/delete; no UI for either).
- **Dashboard**: all KPI cards + Lead Status / Opportunity Pipeline / Monthly Sales Chart.js charts, Today/Week/Month/Custom filter, role-scoped.
- **REST API** (cookie auth; 401/403 JSON, not redirects): `POST /api/auth/login|logout`, `GET/POST/PUT/DELETE /api/customers[/id]`, `GET/POST /api/leads`, `GET/POST /api/opportunities`. DTOs only; 200/201/204/400/401/403/404/409.

## Final acceptance quick-test
1. Browse `/` logged out → redirected to login. 2. Register → dashboard. 3. Create customer with bad email/phone → blocked client-side. 4. Disable JS / crafted POST → server rejects. 5-8. Opportunity amount ≤ 0, probability 101, past close date, past follow-up → rejected. 9-11. Log in as each role and compare menus/data. 12. Check **Audit Log** (Admin). 13. Call `/api/customers` after `POST /api/auth/login`. 14. Dashboard cards/charts.
