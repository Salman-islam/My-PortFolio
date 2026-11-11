using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public static class Domain
{
    static void Apply(Product x, ProductInput input)
    {
        Rules.Require(input.Price >= 0 && input.Price <= 10000000 && decimal.Round(input.Price, 2) == input.Price, "Price must be nonnegative with at most 2 decimal places.");
        Rules.Require(input.ReorderLevel is >= 0 and <= 1000000, "Invalid reorder level.");
        x.Name = Rules.Text(input.Name);
        x.Sku = Rules.Text(input.Sku, 60).ToUpperInvariant();
        x.Category = Rules.Text(input.Category);
        x.Price = input.Price;
        x.ReorderLevel = input.ReorderLevel;
        x.Active = input.Active;
    }
    static void Apply(Party x, PartyInput input)
    {
        Rules.Require(input.Kind is "Customer" or "Supplier", "Choose Customer or Supplier.");
        x.Name = Rules.Text(input.Name);
        x.Email = Rules.Text(input.Email).ToLowerInvariant();
        Rules.Email(x.Email);
        x.Kind = input.Kind;
    }
    static async Task<List<OrderLine>> ValidateOrder(AppDb db, OrderInput input)
    {
        Rules.Require(input.Kind is "Sale" or "Purchase", "Invalid order kind.");
        var party = await db.Parties.FindAsync(input.PartyId) ?? throw Rules.Missing();
        Rules.Require(party.Kind == (input.Kind == "Sale" ? "Customer" : "Supplier"), "Choose the correct customer or supplier.");
        Rules.Require(input.Lines is { Count: > 0 and <= 100 }, "Add between 1 and 100 order lines.");
        Rules.Require(input.Lines!.Select(x => x.ProductId).Distinct().Count() == input.Lines.Count, "A product can appear only once per order.");
        var lines = new List<OrderLine>();
        foreach (var l in input.Lines)
        {
            Rules.Require(l.Quantity is > 0 and <= 1000000 && l.UnitPrice >= 0 && l.UnitPrice <= 10000000 && decimal.Round(l.UnitPrice, 2) == l.UnitPrice, "Invalid quantity or unit price.");
            Rules.Require(await db.Products.AnyAsync(x => x.Id == l.ProductId && x.Active), "Choose an active product.");
            lines.Add(new()
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            });
        }
        return lines;
    }
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapGet("/workspace", async (AppDb db) => { var products = await db.Products.OrderBy(x => x.Name).ToListAsync(); var orders = await db.Orders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(x => x.Product).OrderByDescending(x => x.Id).ToListAsync(); return new { products, parties = await db.Parties.OrderBy(x => x.Name).ToListAsync(), orders = orders.Select(x => new { x.Id, x.Version, x.Kind, x.PartyId, party = x.Party.Name, x.Status, x.CreatedAt, x.PostedAt, total = x.Lines.Sum(l => l.Quantity * l.UnitPrice), lines = x.Lines.Select(l => new { l.ProductId, product = l.Product.Name, l.Quantity, l.UnitPrice, total = l.Quantity * l.UnitPrice }) }), movements = await db.Movements.Include(x => x.Product).OrderByDescending(x => x.Id).Take(500).Select(x => new { x.Id, product = x.Product.Name, x.Delta, x.Reason, x.At, x.OrderId }).ToListAsync(), summary = new[] { new { label = "Active products", value = products.Count(x => x.Active).ToString() }, new { label = "Low-stock alerts", value = products.Count(x => x.Active && x.Stock <= x.ReorderLevel).ToString() }, new { label = "Posted sales", value = orders.Where(x => x.Kind == "Sale" && x.Status == "Posted").Sum(x => x.Lines.Sum(l => l.Quantity * l.UnitPrice)).ToString("N2") }, new { label = "Units on hand", value = products.Sum(x => (long)x.Stock).ToString() } } }; });
        api.MapPost("/products", async (ProductInput input, AppDb db, ClaimsPrincipal u) => { var x = new Product(); Apply(x, input); db.Products.Add(x); db.Audit(u, "Product created", x.Sku); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPut("/products/{id:int}", async (int id, ProductInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Products.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Apply(x, input); db.Audit(u, "Product updated", x.Sku); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapDelete("/products/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Products.FindAsync(id) ?? throw Rules.Missing(); Rules.Require(x.Stock == 0 && !await db.Lines.AnyAsync(l => l.ProductId == id) && !await db.Movements.AnyAsync(l => l.ProductId == id), "This product has history. Deactivate it instead.", 409); db.Products.Remove(x); db.Audit(u, "Product deleted", x.Sku); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/products/{id:int}/adjust", async (int id, AdjustmentInput input, AppDb db, ClaimsPrincipal u) => { await Rules.Atomic(db, async () => { var x = await db.Products.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Rules.Require(input.Delta != 0 && Math.Abs((long)input.Delta) <= 1000000, "Adjustment must be nonzero and within one million units."); Rules.Require((long)x.Stock + input.Delta >= 0 && (long)x.Stock + input.Delta <= int.MaxValue, "Stock cannot become negative or exceed the limit.", 409); x.Stock += input.Delta; db.Movements.Add(new() { ProductId = id, Delta = input.Delta, Reason = Rules.Text(input.Reason, 500) }); db.Audit(u, "Stock adjusted", x.Sku); }); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/parties", async (PartyInput input, AppDb db, ClaimsPrincipal u) => { var x = new Party(); Apply(x, input); db.Parties.Add(x); db.Audit(u, "Contact created", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); });
        api.MapPut("/parties/{id:int}", async (int id, PartyInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Parties.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Rules.Require(x.Kind == input.Kind || !await db.Orders.AnyAsync(o => o.PartyId == id), "Contact type cannot change after orders exist."); Apply(x, input); db.Audit(u, "Contact updated", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); });
        api.MapDelete("/parties/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Parties.FindAsync(id) ?? throw Rules.Missing(); Rules.Require(!await db.Orders.AnyAsync(o => o.PartyId == id), "Contact has orders and cannot be deleted.", 409); db.Parties.Remove(x); db.Audit(u, "Contact deleted", x.Name); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/orders", async (OrderInput input, AppDb db, ClaimsPrincipal u) => { var lines = await ValidateOrder(db, input); var x = new Order { Kind = input.Kind, PartyId = input.PartyId, Lines = lines }; db.Orders.Add(x); db.Audit(u, "Order created", x.Kind); await db.SaveChangesAsync(); return Results.Ok(new { x.Id }); });
        api.MapPut("/orders/{id:int}", async (int id, OrderInput input, AppDb db, ClaimsPrincipal u) => { await Rules.Atomic(db, async () => { var x = await db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Rules.Require(x.Status == "Draft", "Only draft orders can be edited.", 409); var lines = await ValidateOrder(db, input); db.Lines.RemoveRange(x.Lines); await db.SaveChangesAsync(); x.Kind = input.Kind; x.PartyId = input.PartyId; x.Lines = lines; db.Audit(u, "Draft order updated", "Order #" + id); }); return Results.NoContent(); });
        api.MapPost("/orders/{id:int}/post", async (int id, VersionInput input, AppDb db, ClaimsPrincipal u) => { await Rules.Atomic(db, async () => { var x = await db.Orders.Include(x => x.Lines).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Rules.Require(x.Status == "Draft", "This order has already been posted or cancelled.", 409); foreach (var l in x.Lines.OrderBy(x => x.ProductId)) { Rules.Require(l.Product.Active, "An order product is inactive."); var delta = x.Kind == "Sale" ? -l.Quantity : l.Quantity; Rules.Require((long)l.Product.Stock + delta >= 0 && (long)l.Product.Stock + delta <= int.MaxValue, "Insufficient stock or stock limit reached for " + l.Product.Name, 409); l.Product.Stock += delta; db.Movements.Add(new() { ProductId = l.ProductId, OrderId = id, Delta = delta, Reason = x.Kind + " order #" + id }); } x.Status = "Posted"; x.PostedAt = DateTime.UtcNow; db.Audit(u, "Order posted", x.Kind + " #" + id); }); return Results.NoContent(); });
        api.MapPost("/orders/{id:int}/cancel", async (int id, VersionInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Orders.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); Rules.Require(x.Status == "Draft", "Only drafts can be cancelled. Posted transactions retain their history.", 409); x.Status = "Cancelled"; db.Audit(u, "Order cancelled", "Order #" + id); await db.SaveChangesAsync(); return Results.NoContent(); });
    }
    public static async Task Seed(AppDb db, IPasswordHasher<AppUser> h, string password)
    {
        if (await db.Users.AnyAsync())
            return;
        await Accounts.Create(db, h, "Admin User", "admin@example.com", password, "Admin");
        await Accounts.Create(db, h, "Warehouse Staff", "staff@example.com", password, "Staff");
        db.Parties.AddRange(new()
        {
            Name = "Northstar Supplies",
            Email = "orders@northstar.example",
            Kind = "Supplier"
        }, new()
        {
            Name = "Acme Studio",
            Email = "hello@acme.example",
            Kind = "Customer"
        });
        var a = new Product { Name = "Wireless keyboard", Sku = "ACC-1042", Category = "Accessories", Price = 45, Stock = 48, ReorderLevel = 10 };
        var b = new Product { Name = "Standing desk", Sku = "FUR-3010", Category = "Furniture", Price = 320, Stock = 4, ReorderLevel = 5 };
        var c = new Product { Name = "Studio monitor", Sku = "ELC-2001", Category = "Electronics", Price = 220, Stock = 12, ReorderLevel = 5 };
        db.Products.AddRange(a, b, c);
        db.Movements.AddRange(new()
        {
            Product = a,
            Delta = 48,
            Reason = "Opening balance"
        }, new()
        {
            Product = b,
            Delta = 4,
            Reason = "Opening balance"
        }, new()
        {
            Product = c,
            Delta = 12,
            Reason = "Opening balance"
        });
        await db.SaveChangesAsync();
    }
}
