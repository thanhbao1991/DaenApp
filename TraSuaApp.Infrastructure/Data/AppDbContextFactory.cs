using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(Config.ConnectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    // ✅ Thêm method mới không cần args
    public AppDbContext CreateDbContext()
    {
        return CreateDbContext(new string[0]);
    }

}
