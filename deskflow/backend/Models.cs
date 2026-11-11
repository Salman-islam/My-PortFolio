using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public class Category : Entity
{
    public string Name { get; set; } = "";
}
public class Ticket : Entity
{
    public string Title { get; set; } = ""; public string Description { get; set; } = ""; public string Priority { get; set; } = "Medium"; public string Status { get; set; } = "Open"; public int CategoryId
    {
        get; set;
    }
    public Category Category { get; set; } = null!; public int RequesterId
    {
        get; set;
    }
    public AppUser Requester { get; set; } = null!; public int? AssignedToId
    {
        get; set;
    }
    public AppUser? AssignedTo
    {
        get; set;
    }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? ResolvedAt
    {
        get; set;
    }
}
public class Comment : Entity
{
    public int TicketId
    {
        get; set;
    }
    public Ticket Ticket { get; set; } = null!; public int AuthorId
    {
        get; set;
    }
    public AppUser Author { get; set; } = null!; public string Body { get; set; } = ""; public bool Internal
    {
        get; set;
    }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
public class TicketEvent
{
    public int Id
    {
        get; set;
    }
    public int TicketId
    {
        get; set;
    }
    public Ticket Ticket { get; set; } = null!; public string Description { get; set; } = ""; public DateTime At { get; set; } = DateTime.UtcNow;
}
public partial class AppDb
{
    public DbSet<Category> Categories => Set<Category>(); public DbSet<Ticket> Tickets => Set<Ticket>(); public DbSet<Comment> Comments => Set<Comment>(); public DbSet<TicketEvent> Events => Set<TicketEvent>(); partial void ConfigureDomain(ModelBuilder b)
    {
        b.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Ticket>().HasIndex(x => new { x.RequesterId, x.Status });
        b.Entity<Ticket>().Property(x => x.Description).HasMaxLength(4000);
        b.Entity<Comment>().Property(x => x.Body).HasMaxLength(4000);
    }
}
public record TicketInput(string Title, string Description, string Priority, int CategoryId, long Version = 0);
public record AssignInput(int AssignedToId, long Version);
public record StatusInput(string Status, long Version);
public record CommentInput(string Body, bool Internal = false);
