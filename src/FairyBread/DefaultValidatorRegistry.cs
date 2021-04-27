using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FairyBread
{
    public class DefaultValidatorRegistry : IValidatorRegistry
    {
        private static readonly Type _hasOwnScopeInterfaceType = typeof(IRequiresOwnScopeValidator);

        public DefaultValidatorRegistry(IServiceCollection services, IFairyBreadOptions options)
        {
            var validatorResults = new List<AssemblyScanner.AssemblyScanResult>();
            var objectValidatorInterface = typeof(IValidator<object>);
            var underlyingValidatorType = objectValidatorInterface.GetGenericTypeDefinition().UnderlyingSystemType;

            foreach (var service in services)
            {
                if (!service.ServiceType.IsGenericType ||
                    service.ServiceType.Name != objectValidatorInterface.Name ||
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
                if (!Cache.TryGetValue(validatedType, out var validatorsForType))
                {
                    Cache[validatedType] = validatorsForType = new List<ValidatorDescriptor>();
                }

                var requiresOwnScope = ShouldBeResolvedInOwnScope(validatorType);

                var validatorDescriptor = new ValidatorDescriptor(validatorType, requiresOwnScope);

                validatorsForType.Add(validatorDescriptor);
            }
        }

        public Dictionary<Type, List<ValidatorDescriptor>> Cache { get; } = new();

        public bool ShouldBeResolvedInOwnScope(Type validatorType)
            => _hasOwnScopeInterfaceType.IsAssignableFrom(validatorType);
    }
}
