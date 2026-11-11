using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public class Product : Entity
{
    public string Name { get; set; } = ""; public string Sku { get; set; } = ""; public string Category { get; set; } = ""; public decimal Price
    {
        get; set;
    }
    public int Stock
    {
        get; set;
    }
    public int ReorderLevel
    {
        get; set;
    }
    public bool Active { get; set; } = true;
}
public class Party : Entity
{
    public string Name { get; set; } = ""; public string Email { get; set; } = ""; public string Kind { get; set; } = "Customer";
}
public class Order : Entity
{
    public string Kind { get; set; } = "Sale"; public int PartyId
    {
        get; set;
    }
    public Party Party { get; set; } = null!; public string Status { get; set; } = "Draft"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? PostedAt
    {
        get; set;
    }
    public List<OrderLine> Lines { get; set; } = [];
}
public class OrderLine
{
    public int Id
    {
        get; set;
    }
    public int OrderId
    {
        get; set;
    }
    public int ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public int Quantity
    {
        get; set;
    }
    public decimal UnitPrice
    {
        get; set;
    }
}
public class StockMovement
{
    public int Id
    {
        get; set;
    }
    public int ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public int? OrderId
    {
        get; set;
    }
    public Order? Order
    {
        get; set;
    }
    public int Delta
    {
        get; set;
    }
    public string Reason { get; set; } = ""; public DateTime At { get; set; } = DateTime.UtcNow;
}
public partial class AppDb
{
    public DbSet<Product> Products => Set<Product>(); public DbSet<Party> Parties => Set<Party>(); public DbSet<Order> Orders => Set<Order>(); public DbSet<OrderLine> Lines => Set<OrderLine>(); public DbSet<StockMovement> Movements => Set<StockMovement>(); partial void ConfigureDomain(ModelBuilder b)
    {
        b.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        b.Entity<OrderLine>().HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        b.Entity<Order>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
    }
}
public record ProductInput(string Name, string Sku, string Category, decimal Price, int ReorderLevel, bool Active = true, long Version = 0);
public record PartyInput(string Name, string Email, string Kind, long Version = 0);
public record LineInput(int ProductId, int Quantity, decimal UnitPrice);
public record OrderInput(string Kind, int PartyId, List<LineInput> Lines, long Version = 0);
public record AdjustmentInput(int Delta, string Reason, long Version);
