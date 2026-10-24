using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NewsHub.Application.Common.Behaviors;

namespace NewsHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Đăng ký MediatR và Pipeline Behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Đăng ký toàn bộ FluentValidation Validators trong Application assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
