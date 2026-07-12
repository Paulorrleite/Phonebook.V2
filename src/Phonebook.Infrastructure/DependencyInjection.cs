using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Phonebook.Application.Abstractions;
using Phonebook.Infrastructure.Persistence;

namespace Phonebook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Phonebook");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["PHONEBOOK_CONNECTION_STRING"];
        }

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddNpgsql<PhonebookDbContext>(connectionString);
        }

        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IContactReadService, ContactReadService>();
        services.AddScoped<IPhonebookUnitOfWork>(provider => provider.GetRequiredService<PhonebookDbContext>());

        return services;
    }
}
