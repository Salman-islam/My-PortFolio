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
        new Product { Name = "Wireless keyboard", Sku = "ACC-1042", Category = "Accessories", Quantity = 48 },
        new Product { Name = "Studio monitor", Sku = "ELC-2001", Category = "Electronics", Quantity = 12 },
        new Product { Name = "Standing desk", Sku = "FUR-3010", Category = "Furniture", Quantity = 4 },
        new Product { Name = "USB-C hub", Sku = "ACC-1048", Category = "Accessories", Quantity = 32 },
        new Product { Name = "Desk lamp", Sku = "ELC-2018", Category = "Electronics", Quantity = 7 }
    ); db.SaveChanges(); }
}
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/products", async (AppDb db) => Results.Ok(await db.Items.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));
app.MapGet("/api/products/{id:int}", async Task<IResult> (int id, AppDb db) => await db.Items.FindAsync(id) is Product item ? Results.Ok(item) : Results.NotFound());
app.MapPost("/api/products", async Task<IResult> (ProductInput input, AppDb db) => {
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = new Product();
    item.Name = input.Name.Trim();
    item.Sku = input.Sku.Trim();
    item.Category = input.Category.Trim();
    item.Quantity = input.Quantity;
    db.Items.Add(item); await db.SaveChangesAsync();
    return Results.Created($"/api/products/{item.Id}", item);
});
app.MapPut("/api/products/{id:int}", async Task<IResult> (int id, ProductInput input, AppDb db) => {
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Name = input.Name.Trim();
    item.Sku = input.Sku.Trim();
    item.Category = input.Category.Trim();
    item.Quantity = input.Quantity;
    await db.SaveChangesAsync(); return Results.Ok(item);
});
app.MapDelete("/api/products/{id:int}", async Task<IResult> (int id, AppDb db) => {
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Items.Remove(item); await db.SaveChangesAsync(); return Results.NoContent();
});
app.Run();
static Dictionary<string, string[]> Validate(ProductInput input) {
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 160) errors["Name"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Sku) || input.Sku.Length > 160) errors["Sku"] = ["Required; maximum 160 characters."];
    if (string.IsNullOrWhiteSpace(input.Category) || input.Category.Length > 160) errors["Category"] = ["Required; maximum 160 characters."];
    if (input.Quantity < 0) errors["Quantity"] = ["Quantity cannot be negative."];
    if (!new[] { "Electronics", "Furniture", "Accessories" }.Contains(input.Category)) errors["Category"] = ["Choose a valid option."];
    return errors;
}
public class ProductInput {
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Category { get; set; } = "";
    public int Quantity { get; set; }
}
public class Product : ProductInput { public int Id { get; set; } }
public class AppDb(DbContextOptions<AppDb> options) : DbContext(options) {
    public DbSet<Product> Items => Set<Product>();
}
