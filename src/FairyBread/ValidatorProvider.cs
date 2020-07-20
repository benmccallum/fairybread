using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FairyBread
{
    public class ValidatorProvider : IValidatorProvider
    {
        private readonly IFairyBreadOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, List<Type>> _cache = new Dictionary<Type, List<Type>>();

        public ValidatorProvider(IFairyBreadOptions options, IServiceProvider serviceProvider)
        {
            _options = options;
            _serviceProvider = serviceProvider;

            var validators = _serviceProvider.GetServices<IValidator>();
            if (!validators.Any() && options.ThrowIfNoValidatorsFound)
            {
                throw new Exception($"No validators were found. Ensure you've registered some.");
            }

            foreach (var validator in validators)
            {
                var validatorX = validator as IValidator<object>;
                //var validatedType = validator..InterfaceType.GenericTypeArguments.Single();
                //if (!_cache.TryGetValue(validatedType, out var validatorsForType))
                //{
                //    _cache[validatedType] = validatorsForType = new List<Type>();
                //}

                //validatorsForType.Add(validatorType);
            }
        }

        public IEnumerable<IValidator> GetValidators(Type validatorType)
        {
            return _serviceProvider.GetServices(validatorType).Cast<IValidator>();
        }
    }
}
