using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using HotChocolate;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace FairyBread
{
    public class DefaultValidatorRegistry : IValidatorRegistry
    {
        private static readonly Type _hasOwnScopeInterfaceType = typeof(IRequiresOwnScopeValidator);

        public DefaultValidatorRegistry(IServiceCollection services, IFairyBreadOptions options)
        {
            var validatorResults = new List<AssemblyScanner.AssemblyScanResult>();
            var objectValidatorInterfaceType = typeof(IValidator<object>);
            var explicitUsageOnlyValidatorInterfaceType = typeof(IExplicitUsageOnlyValidator);
            var underlyingValidatorType = objectValidatorInterfaceType.GetGenericTypeDefinition().UnderlyingSystemType;

            foreach (var service in services)
            {
                if (!service.ServiceType.IsGenericType ||
                    service.ServiceType.Name != objectValidatorInterfaceType.Name ||
                    explicitUsageOnlyValidatorInterfaceType.IsAssignableFrom(service.ImplementationType) ||
                    service.ServiceType.GetGenericTypeDefinition() != underlyingValidatorType)
                {
                    continue;
                }

                validatorResults.Add(
                    new AssemblyScanner.AssemblyScanResult(
                        service.ServiceType,
                        service.ImplementationType));
            }

            if (!validatorResults.Any() && options.ThrowIfNoValidatorsFound)
            {
                throw new Exception($"No validators were found by FairyBread. " +
                    $"Ensure you're registering your FluentValidation validators for DI.");
            }

            foreach (var validatorResult in validatorResults)
            {
                var validatorType = validatorResult.ValidatorType;

                var validatedType = validatorResult.InterfaceType.GenericTypeArguments.Single();
                if (!CacheByArgType.TryGetValue(validatedType, out var validatorsForType))
                {
                    CacheByArgType[validatedType] = validatorsForType = new List<ValidatorDescriptor>();
                }

                var requiresOwnScope = ShouldBeResolvedInOwnScope(validatorType);

                var validatorDescriptor = new ValidatorDescriptor(validatorType, requiresOwnScope);

                validatorsForType.Add(validatorDescriptor);
            }
        }

        public Dictionary<Type, List<ValidatorDescriptor>> CacheByArgType { get; } = new();
        public Dictionary<FieldCoordinate, List<ValidatorDescriptor>> CacheByFieldCoord { get; } = new();

        public bool ShouldBeResolvedInOwnScope(Type validatorType)
            => _hasOwnScopeInterfaceType.IsAssignableFrom(validatorType);
    }
}
