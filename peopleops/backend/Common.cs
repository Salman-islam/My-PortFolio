using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace Portfolio;

public static class Bootstrap
{
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var b = WebApplication.CreateBuilder(args);
        b.Services.AddDbContext<AppDb>(o => o.UseSqlServer(b.Configuration.GetConnectionString("Default")));
        var key = b.Configuration["Auth:Key"];
        if (string.IsNullOrEmpty(key))
        {
            if (!b.Environment.IsDevelopment() && !b.Environment.IsEnvironment("Testing"))
                throw new InvalidOperationException("Set Auth__Key to at least 32 random characters.");
            key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        }
        if (key.Length < 32)
            throw new InvalidOperationException("Auth key must be at least 32 characters.");
        b.Services.AddSingleton(new TokenService(key));
        b.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = "peopleops",
                ValidateAudience = true,
                ValidAudience = "peopleops",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.FromSeconds(10)
            };
            o.Events = new JwtBearerEvents
            {
                OnTokenValidated = async ctx =>
                {
                    var id = ctx.Principal!.FindFirstValue(ClaimTypes.NameIdentifier);
                    var u = await ctx.HttpContext.RequestServices.GetRequiredService<AppDb>().Users.FindAsync(int.Parse(id!));
                    if (u is null || !u.Active || u.Role != ctx.Principal!.FindFirstValue(ClaimTypes.Role))
                        ctx.Fail("Account is no longer active.");
                }
            };
        });
        b.Services.AddAuthorization();
        b.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        b.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
        configure?.Invoke(b);
        var app = b.Build();
        app.Use(async (ctx, next) =>
        {
            try
            {
                await next(ctx);
            }
            catch (BusinessException ex) { ctx.Response.StatusCode = ex.Status; await ctx.Response.WriteAsJsonAsync(new { message = ex.Message }); }
            catch (DbUpdateConcurrencyException) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "This record changed. Refresh and retry." }); }
            catch (DbUpdateException ex) { app.Logger.LogWarning(ex, "Database write rejected"); ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "The record conflicts with existing data or is still referenced." }); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205 || ex.Number == 1222) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "Another request changed this data. Refresh and retry." }); }
            catch (Exception ex) { app.Logger.LogError(ex, "Request failed"); ctx.Response.StatusCode = 500; await ctx.Response.WriteAsJsonAsync(new { message = "The request failed. See server logs for details." }); }
        });
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/api/health", () => new { status = "ok", project = "peopleops" });
        app.MapPost("/api/auth/login", async (LoginInput input, AppDb db, IPasswordHasher<AppUser> hasher, TokenService tokens) =>
        {
            var email = Rules.Text(input.Email, 160).ToLowerInvariant();
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
            if (u is null || !u.Active || hasher.VerifyHashedPassword(u, u.PasswordHash, input.Password ?? "") == PasswordVerificationResult.Failed)
                throw new BusinessException("Invalid email or password.", 401);
            return Results.Ok(new
            {
                token = tokens.Create(u),
                user = new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role
                },
                expiresIn = 3600
            });
        }).RequireRateLimiting("login");
        app.MapGet("/api/auth/me", async (ClaimsPrincipal user, AppDb db) => { var u = await db.Users.FindAsync(Rules.UserId(user)); return new { u!.Id, u.Name, u.Email, u.Role }; }).RequireAuthorization();
        app.MapGet("/api/users", async (AppDb db) => await db.Users.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Email, x.Role, x.Active }).ToListAsync()).RequireAuthorization(p => p.RequireRole("Admin"));
        app.MapPost("/api/users", async (UserInput input, AppDb db, IPasswordHasher<AppUser> hasher, ClaimsPrincipal actor) =>
        {
            Rules.Require(new[] { "Admin" }.Contains(input.Role), "Choose an allowed role.");
            var u = await Accounts.Create(db, hasher, input.Name, input.Email, input.Password, input.Role);
            db.Audit(actor, "Account created", u.Email);
            await db.SaveChangesAsync();
            return Results.Created($"/api/users/{u.Id}", new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role
            });
        }).RequireAuthorization(p => p.RequireRole("Admin"));
        app.MapPut("/api/users/{id:int}/active", async (int id, ActiveInput input, AppDb db, ClaimsPrincipal actor) =>
        {
            Rules.Require(id != Rules.UserId(actor), "You cannot deactivate your own account.");
            var u = await db.Users.FindAsync(id) ?? throw Rules.Missing();
            u.Active = input.Active;
            db.Audit(actor, "Account status changed", u.Email);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Admin"));
        app.MapGet("/api/audit", async (AppDb db) => await db.Audits.OrderByDescending(x => x.Id).Take(200).ToListAsync()).RequireAuthorization(p => p.RequireRole("Admin"));
        Domain.Map(app);
        return app;
    }
    public static async Task Initialize(WebApplication app, bool testing = false)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        if (testing)
            await db.Database.EnsureCreatedAsync();
        else if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
            await db.Database.MigrateAsync();
        if (app.Environment.IsDevelopment() || testing)
        {
            var password = app.Configuration["Seed:Password"];
            if (!string.IsNullOrEmpty(password))
                await Domain.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>(), password);
        }
    }
}
public class BusinessException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}
public static class Rules
{
    public static void Require(bool condition, string message, int status = 400)
    {
        if (!condition)
            throw new BusinessException(message, status);
    }
    public static BusinessException Missing() => new("Record not found.", 404);
    public static int UserId(ClaimsPrincipal u) => int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static bool Admin(ClaimsPrincipal u) => u.IsInRole("Admin");
    public static bool Staff(ClaimsPrincipal u) => Admin(u) || u.IsInRole("Staff");
    public static string Text(string? s, int max = 160)
    {
        Require(!string.IsNullOrWhiteSpace(s) && s.Trim().Length <= max, $"Required text must be 1–{max} characters.");
        return s!.Trim();
    }
    public static void Email(string s) => Require(new EmailAddressAttribute().IsValid(s), "Enter a valid email address.");
    public static async Task Atomic(AppDb db, Func<Task> change)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await change();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    public static void Version(Entity e, long version) => Require(e.Version == version, "This record changed. Refresh and retry.", 409);
}
public class TokenService(string key)
{
    public string Create(AppUser u) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("peopleops", "peopleops", new[] { new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Name), new Claim(ClaimTypes.Role, u.Role) }, expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)));
}
public static class Accounts
{
    public static async Task<AppUser> Create(AppDb db, IPasswordHasher<AppUser> hasher, string name, string email, string password, string role)
    {
        email = Rules.Text(email).ToLowerInvariant();
        Rules.Email(email);
        Rules.Require(password is { Length: >= 12 and <= 128 }, "Password must contain 12–128 characters.");
        Rules.Require(!await db.Users.AnyAsync(x => x.Email == email), "This email already has an account.", 409);
        var u = new AppUser { Name = Rules.Text(name), Email = email, Role = role };
        u.PasswordHash = hasher.HashPassword(u, password);
        db.Users.Add(u);
        return u;
    }
}
public abstract class Entity
{
    public int Id
    {
        get; set;
    }
    public long Version { get; set; } = 1;
}
public class AppUser : Entity
{
    public string Name { get; set; } = ""; public string Email { get; set; } = ""; [JsonIgnore] public string PasswordHash { get; set; } = ""; public string Role { get; set; } = ""; public bool Active { get; set; } = true;
}
public class AuditEntry
{
    public int Id
    {
        get; set;
    }
    public int ActorId
    {
        get; set;
    }
    public string Actor { get; set; } = ""; public string Action { get; set; } = ""; public string Detail { get; set; } = ""; public DateTime At { get; set; } = DateTime.UtcNow;
}
public partial class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>(); public DbSet<AuditEntry> Audits => Set<AuditEntry>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureDomain(b);
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var p in entity.GetProperties())
            {
                if (p.ClrType == typeof(string) && p.GetMaxLength() == null)
                    p.SetMaxLength(500);
                if (p.ClrType == typeof(decimal))
                {
                    p.SetPrecision(18);
                    p.SetScale(2);
                }
            }
            if (typeof(Entity).IsAssignableFrom(entity.ClrType))
                b.Entity(entity.ClrType).Property(nameof(Entity.Version)).IsConcurrencyToken();
            foreach (var fk in entity.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
    partial void ConfigureDomain(ModelBuilder b);
    public void Audit(ClaimsPrincipal u, string action, string detail) => Audits.Add(new() { ActorId = Rules.UserId(u), Actor = u.Identity?.Name ?? "", Action = action, Detail = detail });
    public override Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        foreach (var e in ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Modified))
            e.Entity.Version++;
        return base.SaveChangesAsync(token);
    }
}
public record LoginInput(string Email, string Password);
public record UserInput(string Name, string Email, string Password, string Role);
public record ActiveInput(bool Active);
public record VersionInput(long Version);
public record NameInput(string Name, long Version = 0);
