using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Phonebook.Infrastructure.Persistence;

public sealed class PhonebookDbContextFactory : IDesignTimeDbContextFactory<PhonebookDbContext>
{
    public PhonebookDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PHONEBOOK_DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Database=phonebook_design_time";

        var optionsBuilder = new DbContextOptionsBuilder<PhonebookDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PhonebookDbContext(optionsBuilder.Options);
    }
}
