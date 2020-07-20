using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace FairyBread
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddFairyBread(
            this IServiceCollection services,
            Action<IFairyBreadOptions>? configureOptions = null)
        {
            var options = new FairyBreadOptions();
            configureOptions?.Invoke(options);
            services.TryAddSingleton<IFairyBreadOptions>(options);

            services.TryAddSingleton<IValidatorProvider, ValidatorProvider>();
            
            services.TryAddSingleton<IValidationResultHandler, ValidationResultHandler>();

            return services;
        }
    }
}
