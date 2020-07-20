using HotChocolate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace FairyBread
{
    public static class ISchemaBuilderExtensions
    {
        public static ISchemaBuilder AddFairyBread(
            this ISchemaBuilder schemaBuilder, 
            IServiceCollection services,
            Action<FairyBreadOptions>? configureOptions = null)
        {
            var options = new FairyBreadOptions();
            configureOptions?.Invoke(options);
            services.TryAddSingleton<IFairyBreadOptions>(options);

            services.TryAddSingleton<IValidatorProvider, ValidatorProvider>();
            
            services.TryAddSingleton<IValidationResultHandler, ValidationResultHandler>();

            return schemaBuilder;
        }
    }
}
