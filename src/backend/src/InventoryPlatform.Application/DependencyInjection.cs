using FluentValidation;
using InventoryPlatform.Application.Common;
using InventoryPlatform.Application.Features.Auth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(c => c.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<SessionService>();
        return services;
    }
}
