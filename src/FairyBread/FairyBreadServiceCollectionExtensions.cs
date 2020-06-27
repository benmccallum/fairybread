using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace FairyBread
{
    public static class FairyBreadServiceCollectionExtensions
    {
        public static IServiceCollection AddFairyBread(this IServiceCollection services, Action<FairyBreadOptions> configureOptions)
        {
            var options = new FairyBreadOptions();
            configureOptions?.Invoke(options);
            services.TryAddSingleton<IFairyBreadOptions>(sp => options);

            if (options.AssembliesToScanForValidators != null)
            {
                services.AddValidatorsFromAssemblies(options.AssembliesToScanForValidators);
            }

            services.TryAddSingleton<IValidatorBag, ValidatorBag>();
            
            services.TryAddSingleton<IValidationResultHandler, ValidationResultHandler>();

            return services;
        }
    }
}
