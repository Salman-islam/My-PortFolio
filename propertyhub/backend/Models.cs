using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Portfolio;

public class Property : Entity
{
    public string Name { get; set; } = ""; public string Address { get; set; } = "";
}
public class Room : Entity
{
    public int PropertyId
    {
        get; set;
    }
    public Property Property { get; set; } = null!; public string Number { get; set; } = ""; public string Type { get; set; } = "";
}
public class Resident : Entity
{
    public int UserId
    {
        get; set;
    }
    public AppUser User { get; set; } = null!; public int PropertyId
    {
        get; set;
    }
    public Property Property { get; set; } = null!; public int? RoomId
    {
        get; set;
    }
    public Room? Room
    {
        get; set;
    }
}
public class Amenity : Entity
{
    public int PropertyId
    {
        get; set;
    }
    public Property Property { get; set; } = null!; public string Name { get; set; } = ""; public int Capacity
    {
        get; set;
    }
    public bool Active { get; set; } = true;
}
public class Booking : Entity
{
    public int AmenityId
    {
        get; set;
    }
    public Amenity Amenity { get; set; } = null!; public int ResidentId
    {
        get; set;
    }
    public Resident Resident { get; set; } = null!; public DateTime Start
    {
        get; set;
    }
    public DateTime End
    {
        get; set;
    }
    public int Guests
    {
        get; set;
    }
    public string Purpose { get; set; } = ""; public string Status { get; set; } = "Confirmed"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Announcement : Entity
{
    public int PropertyId
    {
        get; set;
    }
    public Property Property { get; set; } = null!; public string Title { get; set; } = ""; public string Body { get; set; } = ""; public DateTime StartsAt
    {
        get; set;
    }
    public DateTime EndsAt
    {
        get; set;
    }
}
public partial class AppDb
{
    public DbSet<Property> Properties => Set<Property>(); public DbSet<Room> Rooms => Set<Room>(); public DbSet<Resident> Residents => Set<Resident>(); public DbSet<Amenity> Amenities => Set<Amenity>(); public DbSet<Booking> Bookings => Set<Booking>(); public DbSet<Announcement> Announcements => Set<Announcement>(); partial void ConfigureDomain(ModelBuilder b)
    {
        b.Entity<Room>().HasIndex(x => new { x.PropertyId, x.Number }).IsUnique();
        b.Entity<Resident>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<Resident>().HasIndex(x => x.RoomId).IsUnique().HasFilter("[RoomId] IS NOT NULL");
        b.Entity<Booking>().HasIndex(x => new { x.AmenityId, x.Start, x.End });
        b.Entity<Announcement>().Property(x => x.Body).HasMaxLength(4000);
    }
}
public record PropertyInput(string Name, string Address, long Version = 0);
public record RoomInput(int PropertyId, string Number, string Type, long Version = 0);
public record ResidentInput(string Name, string Email, string Password, int PropertyId, int? RoomId, long Version = 0);
public record AmenityInput(int PropertyId, string Name, int Capacity, bool Active = true, long Version = 0);
public record BookingInput(int AmenityId, int ResidentId, DateTime Start, DateTime End, int Guests, string Purpose);
public record AnnouncementInput(int PropertyId, string Title, string Body, DateTime StartsAt, DateTime EndsAt, long Version = 0);
