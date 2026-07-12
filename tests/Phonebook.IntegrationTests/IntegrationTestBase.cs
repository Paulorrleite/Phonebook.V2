using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phonebook.Application;
using Phonebook.Infrastructure.Persistence;
using Xunit;

namespace Phonebook.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private AsyncServiceScope? _serviceScope;

    protected PhonebookDbContext DbContext { get; private set; } = null!;

    protected IServiceProvider Services => _serviceScope?.ServiceProvider
        ?? throw new InvalidOperationException("The integration test service scope was not initialized.");

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("PHONEBOOK_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PHONEBOOK_TEST_CONNECTION_STRING is required for integration tests.");
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddNpgsql<PhonebookDbContext>(connectionString);
        services.AddScoped<Phonebook.Application.Abstractions.IContactRepository, Phonebook.Infrastructure.Persistence.ContactRepository>();
        services.AddScoped<Phonebook.Application.Abstractions.IContactReadService, Phonebook.Infrastructure.Persistence.ContactReadService>();
        services.AddScoped<Phonebook.Application.Abstractions.IPhonebookUnitOfWork>(provider => provider.GetRequiredService<PhonebookDbContext>());

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);
        _serviceScope = _serviceProvider.CreateAsyncScope();
        DbContext = Services.GetRequiredService<PhonebookDbContext>();

        try
        {
            await DbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await DbContext.Database.MigrateAsync().ConfigureAwait(false);

            var pendingMigrations = await DbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            if (pendingMigrations.Any())
            {
                throw new InvalidOperationException(
                    $"Pending migrations remain after applying migrations: {string.Join(", ", pendingMigrations)}.");
            }

            if (DbContext.Database.HasPendingModelChanges())
            {
                throw new InvalidOperationException("The EF Core model has pending changes that are not represented by a migration.");
            }
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException("Integration test database setup failed. Verify PHONEBOOK_TEST_CONNECTION_STRING and EF Core migrations.", exception);
        }
    }

    public async Task DisposeAsync()
    {
        if (DbContext is not null)
        {
            await DbContext.DisposeAsync().ConfigureAwait(false);
        }

        if (_serviceScope is not null)
        {
            await _serviceScope.Value.DisposeAsync().ConfigureAwait(false);
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        }
    }
}
