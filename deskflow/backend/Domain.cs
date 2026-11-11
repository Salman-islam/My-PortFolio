using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public static class Domain
{
    static async Task<Ticket> Visible(int id, AppDb db, ClaimsPrincipal u)
    {
        var t = await db.Tickets.FindAsync(id) ?? throw Rules.Missing();
        Rules.Require(Rules.Staff(u) || t.RequesterId == Rules.UserId(u), "Access denied.", 403);
        return t;
    }
    static async Task Apply(Ticket t, TicketInput input, AppDb db)
    {
        Rules.Require(new[] { "Low", "Medium", "High", "Urgent" }.Contains(input.Priority), "Invalid priority.");
        Rules.Require(await db.Categories.AnyAsync(x => x.Id == input.CategoryId), "Choose a valid category.");
        t.Title = Rules.Text(input.Title);
        t.Description = Rules.Text(input.Description, 4000);
        t.Priority = input.Priority;
        t.CategoryId = input.CategoryId;
    }
    static void Event(AppDb db, Ticket t, string text) => db.Events.Add(new() { Ticket = t, Description = text });
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/workspace", async (AppDb db, ClaimsPrincipal u) => { var staff = Rules.Staff(u); var uid = Rules.UserId(u); var ts = await db.Tickets.Include(x => x.Requester).Include(x => x.AssignedTo).Include(x => x.Category).Where(x => staff || x.RequesterId == uid).OrderByDescending(x => x.Id).ToListAsync(); return new { categories = await db.Categories.OrderBy(x => x.Name).ToListAsync(), agents = staff ? await db.Users.Where(x => x.Active && (x.Role == "Staff" || x.Role == "Admin")).Select(x => new { x.Id, x.Name }).ToListAsync() : [], tickets = ts.Select(x => new { x.Id, x.Version, x.Title, x.Description, x.Priority, x.Status, x.CategoryId, category = x.Category.Name, x.RequesterId, requester = x.Requester.Name, x.AssignedToId, assignedTo = x.AssignedTo == null ? "Unassigned" : x.AssignedTo.Name, x.CreatedAt, x.ResolvedAt }), performance = ts.Where(x => x.AssignedToId != null).GroupBy(x => x.AssignedToId).Select(g => new { id = g.Key, name = g.First().AssignedTo!.Name, assigned = g.Count(), resolved = g.Count(x => x.ResolvedAt != null), averageHours = Math.Round(g.Where(x => x.ResolvedAt != null).Select(x => (x.ResolvedAt!.Value - x.CreatedAt).TotalHours).DefaultIfEmpty(0).Average(), 1) }), summary = new[] { new { label = "Total tickets", value = ts.Count.ToString() }, new { label = "Open / in progress", value = ts.Count(x => x.Status is "Open" or "In progress").ToString() }, new { label = "High priority active", value = ts.Count(x => (x.Priority is "High" or "Urgent") && (x.Status is "Open" or "In progress")).ToString() }, new { label = "Resolved / closed", value = ts.Count(x => x.Status is "Resolved" or "Closed").ToString() } } }; });
        api.MapGet("/tickets/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { await Visible(id, db, u); var staff = Rules.Staff(u); return new { comments = await db.Comments.Include(x => x.Author).Where(x => x.TicketId == id && (staff || !x.Internal)).OrderBy(x => x.Id).Select(x => new { x.Id, author = x.Author.Name, x.Body, x.Internal, x.At }).ToListAsync(), history = await db.Events.Where(x => x.TicketId == id).OrderBy(x => x.Id).ToListAsync() }; });
        api.MapPost("/tickets", async (TicketInput input, AppDb db, ClaimsPrincipal u) => { var t = new Ticket { RequesterId = Rules.UserId(u) }; await Apply(t, input, db); db.Tickets.Add(t); Event(db, t, "Ticket opened"); db.Audit(u, "Ticket created", t.Title); await db.SaveChangesAsync(); return Results.Ok(new { t.Id }); });
        api.MapPut("/tickets/{id:int}", async (int id, TicketInput input, AppDb db, ClaimsPrincipal u) => { var t = await Visible(id, db, u); Rules.Version(t, input.Version); Rules.Require(t.Status == "Open", "Only open tickets can be edited.", 409); Rules.Require(Rules.Admin(u) || t.RequesterId == Rules.UserId(u) || t.AssignedToId == Rules.UserId(u), "Only the requester, assignee, or admin can edit.", 403); await Apply(t, input, db); Event(db, t, "Ticket details updated"); db.Audit(u, "Ticket updated", "Ticket #" + id); await db.SaveChangesAsync(); return Results.NoContent(); });
        api.MapPost("/tickets/{id:int}/assign", async (int id, AssignInput input, AppDb db, ClaimsPrincipal u) => { var t = await Visible(id, db, u); Rules.Version(t, input.Version); Rules.Require(t.Status is "Open" or "In progress", "Only active tickets can be assigned.", 409); Rules.Require(Rules.Admin(u) || input.AssignedToId == Rules.UserId(u), "Staff can assign tickets only to themselves.", 403); Rules.Require(Rules.Admin(u) || t.AssignedToId == null || t.AssignedToId == Rules.UserId(u), "This ticket is assigned to another agent.", 403); var agent = await db.Users.FindAsync(input.AssignedToId) ?? throw Rules.Missing(); Rules.Require(agent.Active && (agent.Role is "Admin" or "Staff"), "Choose an active support agent."); t.AssignedToId = agent.Id; Event(db, t, "Assigned to " + agent.Name); db.Audit(u, "Ticket assigned", "Ticket #" + id); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPost("/tickets/{id:int}/status", async (int id, StatusInput input, AppDb db, ClaimsPrincipal u) => { var t = await Visible(id, db, u); Rules.Version(t, input.Version); var staff = Rules.Staff(u); if (staff) Rules.Require(Rules.Admin(u) || t.AssignedToId == Rules.UserId(u), "Only the assigned agent or admin can change status.", 403); else Rules.Require(input.Status == "Open" && t.Status == "Resolved", "Customers can only reopen resolved tickets.", 403); var valid = (t.Status, input.Status) is ("Open", "In progress") or ("In progress", "Resolved") or ("Resolved", "Closed") or ("Resolved", "Open"); Rules.Require(valid, "Invalid transition. Use Open → In progress → Resolved → Closed; resolved tickets may be reopened.", 409); Rules.Require(t.AssignedToId != null || input.Status == "Open", "Assign an agent first."); t.Status = input.Status; if (input.Status == "Resolved") t.ResolvedAt = DateTime.UtcNow; if (input.Status == "Open") t.ResolvedAt = null; Event(db, t, "Status changed to " + input.Status); db.Audit(u, "Ticket status changed", "Ticket #" + id + ": " + input.Status); await db.SaveChangesAsync(); return Results.NoContent(); });
        api.MapPost("/tickets/{id:int}/comments", async (int id, CommentInput input, AppDb db, ClaimsPrincipal u) => { var t = await Visible(id, db, u); Rules.Require(t.Status != "Closed", "Closed tickets are read-only.", 409); Rules.Require(!input.Internal || Rules.Staff(u), "Only staff can add internal comments.", 403); db.Comments.Add(new() { TicketId = id, AuthorId = Rules.UserId(u), Body = Rules.Text(input.Body, 4000), Internal = input.Internal }); db.Audit(u, input.Internal ? "Internal note added" : "Comment added", "Ticket #" + id); await db.SaveChangesAsync(); return Results.NoContent(); });
        api.MapPost("/categories", async (NameInput input, AppDb db, ClaimsPrincipal u) => { var x = new Category { Name = Rules.Text(input.Name) }; db.Categories.Add(x); db.Audit(u, "Category created", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPut("/categories/{id:int}", async (int id, NameInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Categories.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); x.Name = Rules.Text(input.Name); db.Audit(u, "Category updated", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapDelete("/categories/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Categories.FindAsync(id) ?? throw Rules.Missing(); Rules.Require(!await db.Tickets.AnyAsync(t => t.CategoryId == id), "Category is in use.", 409); db.Categories.Remove(x); db.Audit(u, "Category deleted", x.Name); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
    }
    public static async Task Seed(AppDb db, IPasswordHasher<AppUser> h, string password)
    {
        if (await db.Users.AnyAsync())
            return;
        await Accounts.Create(db, h, "Admin User", "admin@example.com", password, "Admin");
        var agent = await Accounts.Create(db, h, "Omar Ali", "staff@example.com", password, "Staff");
        var customer = await Accounts.Create(db, h, "Ayesha Khan", "customer@example.com", password, "Customer");
        var c = new Category { Name = "Access & accounts" };
        var it = new Category { Name = "IT equipment" };
        db.Categories.AddRange(c, it, new()
        {
            Name = "General requests"
        });
        var t = new Ticket { Title = "Cannot access the staff portal", Description = "The portal reports that my account cannot access the dashboard.", Priority = "High", Category = c, Requester = customer };
        var t2 = new Ticket { Title = "Meeting room display not connecting", Description = "The display in room B does not connect over HDMI.", Priority = "Medium", Category = it, Requester = customer, AssignedTo = agent, Status = "In progress" };
        db.Tickets.AddRange(t, t2);
        Event(db, t, "Ticket opened");
        Event(db, t2, "Ticket opened");
        Event(db, t2, "Assigned to Omar Ali");
        await db.SaveChangesAsync();
    }
}
