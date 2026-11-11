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
        new Ticket { Title = "Cannot access the staff portal", Requester = "sara@example.com", Priority = "High", Status = "Open" },
        new Ticket { Title = "Update meeting room display", Requester = "omar@example.com", Priority = "Medium", Status = "In progress" },
        new Ticket { Title = "Password reset request", Requester = "ayesha@example.com", Priority = "Low", Status = "Resolved" },
        new Ticket { Title = "Printer connection drops", Requester = "hassan@example.com", Priority = "Medium", Status = "Open" },
        new Ticket { Title = "Add a new team member", Requester = "noor@example.com", Priority = "Low", Status = "In progress" }
    ); db.SaveChanges(); }
}
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/tickets", async (AppDb db) => Results.Ok(await db.Items.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));
app.MapGet("/api/tickets/{id:int}", async Task<IResult> (int id, AppDb db) => await db.Items.FindAsync(id) is Ticket item ? Results.Ok(item) : Results.NotFound());
app.MapPost("/api/tickets", async Task<IResult> (TicketInput input, AppDb db) => {
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = new Ticket();
    item.Title = input.Title.Trim();
    item.Requester = input.Requester.Trim();
    item.Priority = input.Priority.Trim();
    item.Status = input.Status.Trim();
    db.Items.Add(item); await db.SaveChangesAsync();
    return Results.Created($"/api/tickets/{item.Id}", item);
});
app.MapPut("/api/tickets/{id:int}", async Task<IResult> (int id, TicketInput input, AppDb db) => {
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Title = input.Title.Trim();
    item.Requester = input.Requester.Trim();
    item.Priority = input.Priority.Trim();
    item.Status = input.Status.Trim();
    await db.SaveChangesAsync(); return Results.Ok(item);
});
app.MapDelete("/api/tickets/{id:int}", async Task<IResult> (int id, AppDb db) => {
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Items.Remove(item); await db.SaveChangesAsync(); return Results.NoContent();
});
app.Run();
static Dictionary<string, string[]> Validate(TicketInput input) {
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160) errors["Title"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Requester) || input.Requester.Length > 160) errors["Requester"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Priority) || input.Priority.Length > 160) errors["Priority"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Status) || input.Status.Length > 160) errors["Status"] = ["Required; maximum 160 characters."];
    if (!new[] { "Open", "In progress", "Resolved" }.Contains(input.Status)) errors["Status"] = ["Choose a valid status."];
    if (!new[] { "High", "Medium", "Low" }.Contains(input.Priority)) errors["Priority"] = ["Choose a valid option."];
    if (!System.Net.Mail.MailAddress.TryCreate(input.Requester, out _)) errors["Requester"] = ["Enter a valid email address."];
    return errors;
}
public class TicketInput {
    public string Title { get; set; } = "";
    public string Requester { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
}
public class Ticket : TicketInput { public int Id { get; set; } }
public class AppDb(DbContextOptions<AppDb> options) : DbContext(options) {
    public DbSet<Ticket> Items => Set<Ticket>();
}
