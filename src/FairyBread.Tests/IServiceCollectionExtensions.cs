namespace FairyBread.Tests;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddValidator<TValidator, TValidatee>(this IServiceCollection services)
        where TValidator : AbstractValidator<TValidatee>
    {
        // Add the validator as its interface and its self as per
        // how FV does it and so that FairyBread can search it for the registry (via interface)
        // but instantiate it directly from the provider
        // https://github.com/FluentValidation/FluentValidation/blob/0e45f4efbab956d84f425b0b7d207ada516720bd/src/FluentValidation.DependencyInjectionExtensions/ServiceCollectionExtensions.cs#L90
        return services
            .AddTransient<IValidator<TValidatee>, TValidator>()
            .AddTransient<TValidator>();
    }
}
