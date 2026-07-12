using FluentValidation;
using MediatR;
using Phonebook.Application.Contacts.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Phonebook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton<IPostalCodeValidator, PostalCodeValidator>();

        return services;
    }
}
