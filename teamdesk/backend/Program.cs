using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=app.db"));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    if (!db.Items.Any()) { db.Items.AddRange(
        new Employee { Name = "Ayesha Khan", Email = "ayesha@example.com", Department = "Engineering", Status = "Active" },
        new Employee { Name = "Omar Ali", Email = "omar@example.com", Department = "Design", Status = "Active" },
        new Employee { Name = "Sara Ahmed", Email = "sara@example.com", Department = "Operations", Status = "On leave" },
        new Employee { Name = "Hassan Malik", Email = "hassan@example.com", Department = "Engineering", Status = "Active" },
        new Employee { Name = "Noor Fatima", Email = "noor@example.com", Department = "Design", Status = "Active" }
    ); db.SaveChanges(); }
}
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/employees", async (AppDb db) => Results.Ok(await db.Items.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));
app.MapGet("/api/employees/{id:int}", async Task<IResult> (int id, AppDb db) => await db.Items.FindAsync(id) is Employee item ? Results.Ok(item) : Results.NotFound());
app.MapPost("/api/employees", async Task<IResult> (EmployeeInput input, AppDb db) => {
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = new Employee();
    item.Name = input.Name.Trim();
    item.Email = input.Email.Trim();
    item.Department = input.Department.Trim();
    item.Status = input.Status.Trim();
    db.Items.Add(item); await db.SaveChangesAsync();
    return Results.Created($"/api/employees/{item.Id}", item);
});
app.MapPut("/api/employees/{id:int}", async Task<IResult> (int id, EmployeeInput input, AppDb db) => {
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Name = input.Name.Trim();
    item.Email = input.Email.Trim();
    item.Department = input.Department.Trim();
    item.Status = input.Status.Trim();
    await db.SaveChangesAsync(); return Results.Ok(item);
});
app.MapDelete("/api/employees/{id:int}", async Task<IResult> (int id, AppDb db) => {
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Items.Remove(item); await db.SaveChangesAsync(); return Results.NoContent();
});
app.Run();
static Dictionary<string, string[]> Validate(EmployeeInput input) {
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 160) errors["Name"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 160) errors["Email"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Department) || input.Department.Length > 160) errors["Department"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Status) || input.Status.Length > 160) errors["Status"] = ["Required; maximum 160 characters."];
    if (!new[] { "Active", "On leave" }.Contains(input.Status)) errors["Status"] = ["Choose a valid status."];
    if (!new[] { "Engineering", "Design", "Operations" }.Contains(input.Department)) errors["Department"] = ["Choose a valid option."];
    if (!System.Net.Mail.MailAddress.TryCreate(input.Email, out _)) errors["Email"] = ["Enter a valid email address."];
    return errors;
}
public class EmployeeInput {
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Status { get; set; } = "";
}
public class Employee : EmployeeInput { public int Id { get; set; } }
public class AppDb(DbContextOptions<AppDb> options) : DbContext(options) {
    public DbSet<Employee> Items => Set<Employee>();
}
