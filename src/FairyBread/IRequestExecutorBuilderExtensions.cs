namespace Microsoft.Extensions.DependencyInjection;

public static class IRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddFairyBread(
        this IRequestExecutorBuilder requestExecutorBuilder,
        Action<IFairyBreadOptions>? configureOptions = null)
    {
        // Services
        var services = requestExecutorBuilder.Services;

        var options = new DefaultFairyBreadOptions();
        configureOptions?.Invoke(options);
        services.TryAddSingleton<IFairyBreadOptions>(options);

        services.TryAddSingleton<IValidatorRegistry>(sp =>
            new DefaultValidatorRegistry(services, sp.GetRequiredService<IFairyBreadOptions>()));
        services.TryAddSingleton<IValidatorProvider, DefaultValidatorProvider>();

        services.TryAddSingleton<IValidationErrorsHandler, DefaultValidationErrorsHandler>();

        // Executor builder
        requestExecutorBuilder.TryAddTypeInterceptor<ValidationMiddlewareInjector>();

        return requestExecutorBuilder;
    }
}
