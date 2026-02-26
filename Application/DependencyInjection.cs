using System.Reflection;
using Application.Features.Users.Commands.CreateUser;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрация MediatR
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
        });

        // Регистрация AutoMapper
        services.AddAutoMapper(config => { }, Assembly.GetExecutingAssembly());

        return services;
    }
}
