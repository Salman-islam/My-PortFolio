using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public static class Domain
{
    public static int WorkingDays(DateTime start, DateTime end)
    {
        var count = 0;
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            count++;
        return count;
    }
    static async Task<Employee> Own(AppDb db, ClaimsPrincipal user) => await db.Employees.SingleOrDefaultAsync(x => x.UserId == Rules.UserId(user)) ?? throw new BusinessException("This account has no employee profile.", 403);
    static async Task ValidateEmployee(AppDb db, EmployeeInput input)
    {
        Rules.Require(await db.Departments.AnyAsync(x => x.Id == input.DepartmentId), "Choose a valid department.");
        Rules.Require(input.AnnualAllowance is >= 0 and <= 60, "Annual allowance must be 0–60 days.");
        Rules.Require(input.StartDate.Year >= 2000 && input.StartDate.Date <= DateTime.UtcNow.Date.AddYears(1), "Enter a valid employment start date.");
    }
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/workspace", async (AppDb db, ClaimsPrincipal user) =>
        {
            var admin = Rules.Admin(user);
            var uid = Rules.UserId(user);
            var employees = await db.Employees.Include(x => x.User).Include(x => x.Department).Where(x => admin || x.UserId == uid).OrderBy(x => x.Id).ToListAsync();
            var ids = employees.Select(x => x.Id).ToArray();
            var leaves = await db.Leaves.Include(x => x.Employee).ThenInclude(x => x.User).Where(x => ids.Contains(x.EmployeeId)).OrderByDescending(x => x.Id).ToListAsync();
            var attendance = await db.Attendance.Include(x => x.Employee).ThenInclude(x => x.User).Where(x => ids.Contains(x.EmployeeId)).OrderByDescending(x => x.Day).Take(500).ToListAsync();
            var year = DateTime.UtcNow.Year;
            return new
            {
                departments = await db.Departments.OrderBy(x => x.Name).ToListAsync(),
                employees = employees.Select(x => new { x.Id, x.Version, x.User.Name, x.User.Email, x.DepartmentId, department = x.Department.Name, x.JobTitle, x.StartDate, x.AnnualAllowance, active = x.User.Active, remaining = x.AnnualAllowance - leaves.Where(l => l.EmployeeId == x.Id && l.Status == "Approved" && l.Start.Year == year).Sum(l => l.Days) }),
                leaves = leaves.Select(x => new { x.Id, x.Version, x.EmployeeId, employee = x.Employee.User.Name, x.Start, x.End, x.Days, x.Reason, x.Status, x.DecisionNote }),
                attendance = attendance.Select(x => new { x.Id, x.EmployeeId, employee = x.Employee.User.Name, x.Day, x.CheckIn, x.CheckOut, hours = x.CheckOut.HasValue ? Math.Round((x.CheckOut.Value - x.CheckIn).TotalHours, 2) : 0 }),
                summary = new[] { new { label = "Team members", value = employees.Count.ToString() }, new { label = "Pending requests", value = leaves.Count(x => x.Status == "Pending").ToString() }, new { label = "Checked in today", value = attendance.Count(x => x.Day == DateTime.UtcNow.Date).ToString() }, new { label = "Approved days this year", value = leaves.Where(x => x.Status == "Approved" && x.Start.Year == year).Sum(x => x.Days).ToString() } }
            };
        });
        api.MapPost("/departments", async (NameInput input, AppDb db, ClaimsPrincipal u) => { var x = new Department { Name = Rules.Text(input.Name) }; db.Departments.Add(x); db.Audit(u, "Department created", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPut("/departments/{id:int}", async (int id, NameInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Departments.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); x.Name = Rules.Text(input.Name); db.Audit(u, "Department updated", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapDelete("/departments/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Departments.FindAsync(id) ?? throw Rules.Missing(); Rules.Require(!await db.Employees.AnyAsync(e => e.DepartmentId == id), "Reassign employees before deleting a department.", 409); db.Departments.Remove(x); db.Audit(u, "Department deleted", x.Name); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/employees", async (EmployeeInput input, AppDb db, IPasswordHasher<AppUser> hasher, ClaimsPrincipal u) =>
        {
            await ValidateEmployee(db, input);
            var account = await Accounts.Create(db, hasher, input.Name, input.Email, input.Password, "Employee");
            var x = new Employee { User = account, DepartmentId = input.DepartmentId, JobTitle = Rules.Text(input.JobTitle), StartDate = input.StartDate.Date, AnnualAllowance = input.AnnualAllowance };
            db.Employees.Add(x);
            db.Audit(u, "Employee created", account.Email);
            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                x.Id
            });
        }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPut("/employees/{id:int}", async (int id, EmployeeInput input, AppDb db, ClaimsPrincipal u) =>
        {
            await Rules.Atomic(db, async () => { await ValidateEmployee(db, input); var x = await db.Employees.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); var used = await db.Leaves.Where(l => l.EmployeeId == id && l.Status == "Approved" && l.Start.Year == DateTime.UtcNow.Year).SumAsync(l => l.Days); Rules.Require(input.AnnualAllowance >= used, "Allowance cannot be below already approved days."); var email = Rules.Text(input.Email).ToLowerInvariant(); Rules.Email(email); x.User.Name = Rules.Text(input.Name); x.User.Email = email; x.DepartmentId = input.DepartmentId; x.JobTitle = Rules.Text(input.JobTitle); x.StartDate = input.StartDate.Date; x.AnnualAllowance = input.AnnualAllowance; db.Audit(u, "Employee updated", email); });
            return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/attendance/check-in", async (AppDb db, ClaimsPrincipal u) => { var e = await Own(db, u); var now = DateTime.UtcNow; Rules.Require(e.StartDate <= now.Date, "Employment has not started."); Rules.Require(!await db.Attendance.AnyAsync(x => x.EmployeeId == e.Id && x.Day == now.Date), "You have already checked in today.", 409); Rules.Require(!await db.Attendance.AnyAsync(x => x.EmployeeId == e.Id && x.CheckOut == null), "Check out of your previous attendance record first.", 409); db.Attendance.Add(new() { EmployeeId = e.Id, Day = now.Date, CheckIn = now }); db.Audit(u, "Checked in", now.ToString("O")); await db.SaveChangesAsync(); return Results.NoContent(); });
        api.MapPost("/attendance/check-out", async (AppDb db, ClaimsPrincipal u) => { var e = await Own(db, u); var x = await db.Attendance.SingleOrDefaultAsync(x => x.EmployeeId == e.Id && x.CheckOut == null) ?? throw new BusinessException("No open attendance record."); x.CheckOut = DateTime.UtcNow; db.Audit(u, "Checked out", x.Day.ToString("yyyy-MM-dd")); await db.SaveChangesAsync(); return Results.NoContent(); });
        api.MapPost("/leaves", async (LeaveInput input, AppDb db, ClaimsPrincipal u) =>
        {
            await Rules.Atomic(db, async () => { var e = await db.Employees.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == input.EmployeeId) ?? throw Rules.Missing(); Rules.Require(Rules.Admin(u) || e.UserId == Rules.UserId(u), "You can only request your own leave.", 403); Rules.Require(e.User.Active, "Employee account is inactive."); var start = input.Start.Date; var end = input.End.Date; Rules.Require(start >= DateTime.UtcNow.Date && start >= e.StartDate && end >= start && end.Year == start.Year && start.Year <= DateTime.UtcNow.Year + 1, "Use a future date range within one calendar year."); var days = WorkingDays(start, end); Rules.Require(days > 0 && days <= e.AnnualAllowance, "The range must contain working days within the annual allowance."); Rules.Require(!await db.Leaves.AnyAsync(l => l.EmployeeId == e.Id && (l.Status == "Pending" || l.Status == "Approved") && l.Start <= end && l.End >= start), "This leave overlaps an existing request.", 409); db.Leaves.Add(new() { EmployeeId = e.Id, Start = start, End = end, Days = days, Reason = Rules.Text(input.Reason, 500) }); db.Audit(u, "Leave requested", e.User.Name); });
            return Results.NoContent();
        });
        api.MapPost("/leaves/{id:int}/decision", async (int id, DecisionInput input, AppDb db, ClaimsPrincipal u) =>
        {
            await Rules.Atomic(db, async () => { var x = await db.Leaves.Include(x => x.Employee).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Rules.Require(x.Employee.UserId != Rules.UserId(u), "You cannot approve your own leave.", 403); Rules.Require(x.Status == "Pending", "Only pending requests can be decided.", 409); Rules.Require(input.Status is "Approved" or "Rejected", "Choose Approved or Rejected."); if (input.Status == "Approved") { Rules.Require(x.Start >= DateTime.UtcNow.Date, "The request has already started."); var used = await db.Leaves.Where(l => l.EmployeeId == x.EmployeeId && l.Status == "Approved" && l.Start.Year == x.Start.Year).SumAsync(l => l.Days); Rules.Require(used + x.Days <= x.Employee.AnnualAllowance, "Insufficient remaining leave balance.", 409); } x.Status = input.Status; x.DecisionNote = Rules.Text(input.Note, 500); db.Audit(u, "Leave " + x.Status, "Request #" + id); });
            return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/leaves/{id:int}/cancel", async (int id, VersionInput input, AppDb db, ClaimsPrincipal u) =>
        {
            await Rules.Atomic(db, async () => { var x = await db.Leaves.Include(x => x.Employee).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Require(Rules.Admin(u) || x.Employee.UserId == Rules.UserId(u), "Access denied.", 403); Rules.Version(x, input.Version); Rules.Require(x.Status is "Pending" or "Approved", "Only pending or approved leave can be cancelled.", 409); Rules.Require(x.Start >= DateTime.UtcNow.Date, "Started leave cannot be cancelled."); x.Status = "Cancelled"; db.Audit(u, "Leave cancelled", "Request #" + id); });
            return Results.NoContent();
        });
    }
    public static async Task Seed(AppDb db, IPasswordHasher<AppUser> h, string password)
    {
        if (await db.Users.AnyAsync())
            return;
        await Accounts.Create(db, h, "Admin User", "admin@example.com", password, "Admin");
        var a = await Accounts.Create(db, h, "Ayesha Khan", "employee@example.com", password, "Employee");
        var b = await Accounts.Create(db, h, "Omar Ali", "omar@example.com", password, "Employee");
        var eng = new Department { Name = "Engineering" };
        var ops = new Department { Name = "Operations" };
        db.Departments.AddRange(eng, ops, new()
        {
            Name = "Design"
        });
        var e = new Employee { User = a, Department = eng, JobTitle = "Software Engineer", StartDate = DateTime.UtcNow.Date.AddYears(-1), AnnualAllowance = 24 };
        db.Employees.AddRange(e, new()
        {
            User = b,
            Department = ops,
            JobTitle = "Operations Lead",
            StartDate = DateTime.UtcNow.Date.AddMonths(-8),
            AnnualAllowance = 20
        });
        var start = DateTime.UtcNow.Date.AddDays(7);
        while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            start = start.AddDays(1);
        db.Leaves.Add(new()
        {
            Employee = e,
            Start = start,
            End = start.AddDays(1),
            Days = WorkingDays(start, start.AddDays(1)),
            Reason = "Family visit",
            Status = "Pending"
        });
        await db.SaveChangesAsync();
    }
}
