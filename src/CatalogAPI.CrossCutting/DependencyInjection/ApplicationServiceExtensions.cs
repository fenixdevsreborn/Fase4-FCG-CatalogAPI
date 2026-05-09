using CatalogAPI.Application.Mappings;
using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CatalogAPI.CrossCutting.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.Load("CatalogAPI.Application");

        // Add Mediator with Scoped lifetime to work with DbContext
        services.AddMediator(options =>
        {
            options.Namespace = "CatalogAPI.Application";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(applicationAssembly);

        // Configure Mapster
        MappingConfig.RegisterMappings();
        TypeAdapterConfig.GlobalSettings.Scan(applicationAssembly);

        return services;
    }
}
