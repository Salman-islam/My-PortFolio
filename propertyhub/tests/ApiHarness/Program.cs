using Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
// SQLite is a test fixture only. The application always uses SQL Server.
var app=Bootstrap.Build(args,b=>{
 b.Services.RemoveAll<DbContextOptions<AppDb>>();
 b.Services.RemoveAll<IDbContextOptionsConfiguration<AppDb>>();
 b.Services.AddDbContext<AppDb>(o=>o.UseSqlite("Data Source="+(b.Configuration["TestDb"]??"tests.db")));
 b.Configuration["Seed:Password"]="PortfolioDemo!2026";
});
await Bootstrap.Initialize(app,testing:true);
await app.RunAsync();
