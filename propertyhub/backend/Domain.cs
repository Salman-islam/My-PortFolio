using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public static class Domain
{
    static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    static async Task<int?> OwnProperty(AppDb db, ClaimsPrincipal u) => await db.Residents.Where(x => x.UserId == Rules.UserId(u)).Select(x => (int?)x.PropertyId).SingleOrDefaultAsync();
    static async Task ValidateRoom(AppDb db, int propertyId, int? roomId, int? residentId = null)
    {
        Rules.Require(await db.Properties.AnyAsync(x => x.Id == propertyId), "Choose a valid property.");
        if (roomId.HasValue)
        {
            Rules.Require(await db.Rooms.AnyAsync(x => x.Id == roomId && x.PropertyId == propertyId), "Room must belong to the selected property.");
            Rules.Require(!await db.Residents.AnyAsync(x => x.RoomId == roomId && x.Id != residentId), "This room already has a resident profile.", 409);
        }
    }
    static void Times(DateTime start, DateTime end)
    {
        Rules.Require(start >= DateTime.UtcNow && end > start && end - start <= TimeSpan.FromHours(4) && start <= DateTime.UtcNow.AddDays(30), "Book a future slot within 30 days, lasting at most 4 hours.");
    }
    static async Task<Amenity> Resource(AppDb db, int id, ClaimsPrincipal u)
    {
        var x = await db.Amenities.FindAsync(id) ?? throw Rules.Missing();
        Rules.Require(Rules.Staff(u) || x.PropertyId == await OwnProperty(db, u), "This amenity belongs to another property.", 403);
        Rules.Require(x.Active, "This amenity is unavailable.");
        return x;
    }
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/workspace", async (AppDb db, ClaimsPrincipal u) => { var staff = Rules.Staff(u); var uid = Rules.UserId(u); var pid = await OwnProperty(db, u); var bookings = await db.Bookings.Include(x => x.Amenity).Include(x => x.Resident).ThenInclude(x => x.User).Where(x => staff || x.Resident.UserId == uid).OrderByDescending(x => x.Id).ToListAsync(); var rooms = await db.Rooms.Include(x => x.Property).Where(x => staff || db.Residents.Any(r => r.RoomId == x.Id && r.UserId == uid)).ToListAsync(); var residents = await db.Residents.Include(x => x.User).Include(x => x.Property).Include(x => x.Room).Where(x => staff || x.UserId == uid).ToListAsync(); var amenities = await db.Amenities.Include(x => x.Property).Where(x => staff || x.PropertyId == pid).ToListAsync(); var now = DateTime.UtcNow; return new { properties = await db.Properties.Where(x => staff || x.Id == pid).OrderBy(x => x.Name).ToListAsync(), rooms = rooms.Select(x => new { x.Id, x.Version, x.PropertyId, property = x.Property.Name, x.Number, x.Type, occupied = residents.Any(r => r.RoomId == x.Id) }), residents = residents.Select(x => new { x.Id, x.Version, x.User.Name, x.User.Email, x.PropertyId, property = x.Property.Name, x.RoomId, room = x.Room == null ? "Unassigned" : x.Room.Number }), amenities = amenities.Select(x => new { x.Id, x.Version, x.PropertyId, property = x.Property.Name, x.Name, x.Capacity, x.Active }), bookings = bookings.Select(x => new { x.Id, x.Version, x.AmenityId, amenity = x.Amenity.Name, x.ResidentId, resident = x.Resident.User.Name, x.Start, x.End, x.Guests, x.Purpose, x.Status }), announcements = await db.Announcements.Include(x => x.Property).Where(x => staff || (x.PropertyId == pid && x.StartsAt <= now && x.EndsAt > now)).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Version, x.PropertyId, property = x.Property.Name, x.Title, x.Body, x.StartsAt, x.EndsAt }).ToListAsync(), summary = new[] { new { label = "Properties", value = (await db.Properties.CountAsync(x => staff || x.Id == pid)).ToString() }, new { label = "Residents", value = residents.Count.ToString() }, new { label = "Active amenities", value = amenities.Count(x => x.Active).ToString() }, new { label = "Upcoming bookings", value = bookings.Count(x => x.Status == "Confirmed" && x.End > now).ToString() } } }; });
        api.MapPost("/properties", async (PropertyInput input, AppDb db, ClaimsPrincipal u) => { var x = new Property { Name = Rules.Text(input.Name), Address = Rules.Text(input.Address, 500) }; db.Properties.Add(x); db.Audit(u, "Property created", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPut("/properties/{id:int}", async (int id, PropertyInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Properties.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); x.Name = Rules.Text(input.Name); x.Address = Rules.Text(input.Address, 500); db.Audit(u, "Property updated", x.Name); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapDelete("/properties/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Properties.FindAsync(id) ?? throw Rules.Missing(); db.Properties.Remove(x); db.Audit(u, "Property deleted", x.Name); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/rooms", async (RoomInput input, AppDb db, ClaimsPrincipal u) => { await ValidateRoom(db, input.PropertyId, null); var x = new Room { PropertyId = input.PropertyId, Number = Rules.Text(input.Number, 40), Type = Rules.Text(input.Type) }; db.Rooms.Add(x); db.Audit(u, "Room created", x.Number); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPut("/rooms/{id:int}", async (int id, RoomInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Rooms.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); await ValidateRoom(db, input.PropertyId, null); Rules.Require(x.PropertyId == input.PropertyId || !await db.Residents.AnyAsync(r => r.RoomId == id), "Cannot move an occupied room to another property.", 409); x.PropertyId = input.PropertyId; x.Number = Rules.Text(input.Number, 40); x.Type = Rules.Text(input.Type); db.Audit(u, "Room updated", x.Number); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapDelete("/rooms/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Rooms.FindAsync(id) ?? throw Rules.Missing(); db.Rooms.Remove(x); db.Audit(u, "Room deleted", x.Number); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapPost("/residents", async (ResidentInput input, AppDb db, IPasswordHasher<AppUser> h, ClaimsPrincipal u) => { await Rules.Atomic(db, async () => { await ValidateRoom(db, input.PropertyId, input.RoomId); var account = await Accounts.Create(db, h, input.Name, input.Email, input.Password, "Resident"); db.Residents.Add(new() { User = account, PropertyId = input.PropertyId, RoomId = input.RoomId }); db.Audit(u, "Resident created", account.Email); }); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPut("/residents/{id:int}", async (int id, ResidentInput input, AppDb db, ClaimsPrincipal u) => { await Rules.Atomic(db, async () => { var x = await db.Residents.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); await ValidateRoom(db, input.PropertyId, input.RoomId, id); Rules.Require(x.PropertyId == input.PropertyId || !await db.Bookings.AnyAsync(b => b.ResidentId == id && b.Status == "Confirmed" && b.End > DateTime.UtcNow), "Cancel upcoming bookings before moving properties.", 409); x.User.Name = Rules.Text(input.Name); x.User.Email = Rules.Text(input.Email).ToLowerInvariant(); Rules.Email(x.User.Email); x.PropertyId = input.PropertyId; x.RoomId = input.RoomId; db.Audit(u, "Resident updated", x.User.Email); }); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPost("/amenities", async (AmenityInput input, AppDb db, ClaimsPrincipal u) => { await ValidateRoom(db, input.PropertyId, null); Rules.Require(input.Capacity is > 0 and <= 500, "Capacity must be 1–500."); var x = new Amenity { PropertyId = input.PropertyId, Name = Rules.Text(input.Name), Capacity = input.Capacity, Active = input.Active }; db.Amenities.Add(x); db.Audit(u, "Amenity created", x.Name); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPut("/amenities/{id:int}", async (int id, AmenityInput input, AppDb db, ClaimsPrincipal u) => { await Rules.Atomic(db, async () => { var x = await db.Amenities.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); await ValidateRoom(db, input.PropertyId, null); Rules.Require(input.Capacity is > 0 and <= 500, "Capacity must be 1–500."); Rules.Require(x.PropertyId == input.PropertyId || !await db.Bookings.AnyAsync(b => b.AmenityId == id), "Amenities with booking history cannot change property.", 409); Rules.Require(!await db.Bookings.AnyAsync(b => b.AmenityId == id && b.Status == "Confirmed" && b.End > DateTime.UtcNow && (!input.Active || b.Guests > input.Capacity)), "Cancel affected upcoming bookings before changing capacity or availability.", 409); x.Name = Rules.Text(input.Name); x.PropertyId = input.PropertyId; x.Capacity = input.Capacity; x.Active = input.Active; db.Audit(u, "Amenity updated", x.Name); }); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapDelete("/amenities/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Amenities.FindAsync(id) ?? throw Rules.Missing(); db.Amenities.Remove(x); db.Audit(u, "Amenity deleted", x.Name); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));
        api.MapGet("/availability", async (int amenityId, DateTime start, DateTime end, AppDb db, ClaimsPrincipal u) => { start = Utc(start); end = Utc(end); await Resource(db, amenityId, u); Times(start, end); return new { available = !await db.Bookings.AnyAsync(b => b.AmenityId == amenityId && b.Status == "Confirmed" && b.Start < end && b.End > start) }; });
        api.MapPost("/bookings", async (BookingInput input, AppDb db, ClaimsPrincipal u) => { input = input with { Start = Utc(input.Start), End = Utc(input.End) }; await Rules.Atomic(db, async () => { var amenity = await Resource(db, input.AmenityId, u); var resident = await db.Residents.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == input.ResidentId) ?? throw Rules.Missing(); Rules.Require(Rules.Staff(u) || resident.UserId == Rules.UserId(u), "You can only book for yourself.", 403); Rules.Require(resident.User.Active && resident.PropertyId == amenity.PropertyId, "Choose an active resident in the amenity's property."); Times(input.Start, input.End); Rules.Require(input.Guests > 0 && input.Guests <= amenity.Capacity, "Guest count exceeds capacity or is invalid."); Rules.Require(!await db.Bookings.AnyAsync(b => b.AmenityId == amenity.Id && b.Status == "Confirmed" && b.Start < input.End && b.End > input.Start), "This time slot is already booked.", 409); db.Bookings.Add(new() { AmenityId = amenity.Id, ResidentId = resident.Id, Start = input.Start, End = input.End, Guests = input.Guests, Purpose = Rules.Text(input.Purpose, 500) }); db.Audit(u, "Booking confirmed", amenity.Name); }); return Results.NoContent(); });
        api.MapPost("/bookings/{id:int}/cancel", async (int id, VersionInput input, AppDb db, ClaimsPrincipal u) => { var x = await db.Bookings.Include(x => x.Resident).SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Missing(); Rules.Require(Rules.Staff(u) || x.Resident.UserId == Rules.UserId(u), "Access denied.", 403); Rules.Version(x, input.Version); Rules.Require(x.Status == "Confirmed" && x.Start > DateTime.UtcNow, "Only future confirmed bookings can be cancelled.", 409); x.Status = "Cancelled"; db.Audit(u, "Booking cancelled", "Booking #" + id); await db.SaveChangesAsync(); return Results.NoContent(); });
        api.MapPost("/announcements", async (AnnouncementInput input, AppDb db, ClaimsPrincipal u) => { input = input with { StartsAt = Utc(input.StartsAt), EndsAt = Utc(input.EndsAt) }; await ValidateRoom(db, input.PropertyId, null); Rules.Require(input.EndsAt > input.StartsAt, "End must be after start."); var x = new Announcement { PropertyId = input.PropertyId, Title = Rules.Text(input.Title), Body = Rules.Text(input.Body, 4000), StartsAt = input.StartsAt, EndsAt = input.EndsAt }; db.Announcements.Add(x); db.Audit(u, "Announcement created", x.Title); await db.SaveChangesAsync(); return Results.Ok(x); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapPut("/announcements/{id:int}", async (int id, AnnouncementInput input, AppDb db, ClaimsPrincipal u) => { input = input with { StartsAt = Utc(input.StartsAt), EndsAt = Utc(input.EndsAt) }; var x = await db.Announcements.FindAsync(id) ?? throw Rules.Missing(); Rules.Version(x, input.Version); await ValidateRoom(db, input.PropertyId, null); Rules.Require(input.EndsAt > input.StartsAt, "End must be after start."); x.PropertyId = input.PropertyId; x.Title = Rules.Text(input.Title); x.Body = Rules.Text(input.Body, 4000); x.StartsAt = input.StartsAt; x.EndsAt = input.EndsAt; db.Audit(u, "Announcement updated", x.Title); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
        api.MapDelete("/announcements/{id:int}", async (int id, AppDb db, ClaimsPrincipal u) => { var x = await db.Announcements.FindAsync(id) ?? throw Rules.Missing(); db.Announcements.Remove(x); db.Audit(u, "Announcement deleted", x.Title); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin", "Staff"));
    }
    public static async Task Seed(AppDb db, IPasswordHasher<AppUser> h, string password)
    {
        if (await db.Users.AnyAsync())
            return;
        await Accounts.Create(db, h, "Admin User", "admin@example.com", password, "Admin");
        await Accounts.Create(db, h, "Property Staff", "staff@example.com", password, "Staff");
        var user = await Accounts.Create(db, h, "Ayesha Khan", "resident@example.com", password, "Resident");
        var p = new Property { Name = "Maple Heights", Address = "14 Park Avenue, Lahore" };
        var p2 = new Property { Name = "Cedar Court", Address = "22 Lake Road, Islamabad" };
        db.Properties.AddRange(p, p2);
        var r = new Room { Property = p, Number = "A-101", Type = "Two-bedroom apartment" };
        db.Rooms.AddRange(r, new()
        {
            Property = p,
            Number = "A-102",
            Type = "Studio apartment"
        }, new()
        {
            Property = p2,
            Number = "C-201",
            Type = "Two-bedroom apartment"
        });
        var resident = new Resident { User = user, Property = p, Room = r };
        db.Residents.Add(resident);
        var a = new Amenity { Property = p, Name = "Conference room", Capacity = 12 };
        db.Amenities.AddRange(a, new()
        {
            Property = p,
            Name = "Rooftop lounge",
            Capacity = 30
        }, new()
        {
            Property = p2,
            Name = "Meeting suite",
            Capacity = 8
        });
        db.Bookings.Add(new()
        {
            Amenity = a,
            Resident = resident,
            Start = DateTime.UtcNow.Date.AddDays(2).AddHours(10),
            End = DateTime.UtcNow.Date.AddDays(2).AddHours(11),
            Guests = 5,
            Purpose = "Team planning session"
        });
        db.Announcements.Add(new()
        {
            Property = p,
            Title = "Welcome to your resident workspace",
            Body = "Book shared amenities, review your reservations, and keep up with property announcements.",
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt = DateTime.UtcNow.AddMonths(1)
        });
        await db.SaveChangesAsync();
    }
}
