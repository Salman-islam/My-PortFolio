using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Portfolio;

public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? @"Server=(localdb)\MSSQLLocalDB;Database=PeopleOps;Trusted_Connection=True;TrustServerCertificate=True;";
        return new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlServer(connection).Options);
    }
}
