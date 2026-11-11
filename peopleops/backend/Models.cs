using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public class Department : Entity
{
    public string Name { get; set; } = "";
}
public class Employee : Entity
{
    public int UserId
    {
        get; set;
    }
    public AppUser User { get; set; } = null!; public int DepartmentId
    {
        get; set;
    }
    public Department Department { get; set; } = null!; public string JobTitle { get; set; } = ""; public DateTime StartDate
    {
        get; set;
    }
    public int AnnualAllowance { get; set; } = 20;
}
public class Attendance : Entity
{
    public int EmployeeId
    {
        get; set;
    }
    public Employee Employee { get; set; } = null!; public DateTime Day
    {
        get; set;
    }
    public DateTime CheckIn
    {
        get; set;
    }
    public DateTime? CheckOut
    {
        get; set;
    }
}
public class LeaveRequest : Entity
{
    public int EmployeeId
    {
        get; set;
    }
    public Employee Employee { get; set; } = null!; public DateTime Start
    {
        get; set;
    }
    public DateTime End
    {
        get; set;
    }
    public int Days
    {
        get; set;
    }
    public string Reason { get; set; } = ""; public string Status { get; set; } = "Pending"; public string DecisionNote { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public partial class AppDb
{
    public DbSet<Department> Departments => Set<Department>(); public DbSet<Employee> Employees => Set<Employee>(); public DbSet<Attendance> Attendance => Set<Attendance>(); public DbSet<LeaveRequest> Leaves => Set<LeaveRequest>();
    partial void ConfigureDomain(ModelBuilder b)
    {
        b.Entity<Department>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Employee>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<Attendance>().HasIndex(x => new { x.EmployeeId, x.Day }).IsUnique();
        b.Entity<LeaveRequest>().HasIndex(x => new { x.EmployeeId, x.Start });
    }
}
public record EmployeeInput(string Name, string Email, string Password, int DepartmentId, string JobTitle, DateTime StartDate, int AnnualAllowance, long Version = 0);
public record LeaveInput(int EmployeeId, DateTime Start, DateTime End, string Reason);
public record DecisionInput(string Status, string Note, long Version);
