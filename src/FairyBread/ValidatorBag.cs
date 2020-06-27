using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FairyBread
{
    public class ValidatorBag : IValidatorBag
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, List<Type>> _cache = new Dictionary<Type, List<Type>>();

        public ValidatorBag(IServiceProvider serviceProvider, IFairyBreadOptions options)
        {
            _serviceProvider = serviceProvider;

            var validatorResults = AssemblyScanner.FindValidatorsInAssemblies(options.AssembliesToScanForValidators).ToArray();

            if (!validatorResults.Any() && options.ThrowIfNoValidatorsFound)
            {
                throw new Exception($"No validators were found in the provided " +
                    $"{nameof(IFairyBreadOptions)}.{nameof(IFairyBreadOptions.AssembliesToScanForValidators)} which included: " +
                    $"{string.Join(",", options.AssembliesToScanForValidators)}.");
            }

            foreach (var validatorResult in validatorResults)
            {
                var validatorType = validatorResult.ValidatorType;
                if (validatorType.IsAbstract)
                {
                    continue;
                }

                var validatedType = validatorResult.InterfaceType.GenericTypeArguments.Single();
                if (!_cache.TryGetValue(validatedType, out var validatorsForType))
                {
                    _cache[validatedType] = validatorsForType = new List<Type>();
                }

                validatorsForType.Add(validatorType);
            }
        }

        public IEnumerable<IValidator> GetValidators(Type typeToValidate)
        {
            return _cache.TryGetValue(typeToValidate, out var validatorTypes)
                ? validatorTypes.Select(t => (IValidator)_serviceProvider.GetService(t))
                : Enumerable.Empty<IValidator>();
        }
    }
}
