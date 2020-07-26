using FluentValidation;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FairyBread
{
    public class DefaultValidatorProvider : IValidatorProvider
    {
        protected readonly IServiceProvider ServiceProvider;
        protected static readonly Type HasOwnScopeInterfaceType = typeof(IRequiresOwnScopeValidator);
        protected readonly Dictionary<Type, List<ValidatorDescriptor>> Cache = new Dictionary<Type, List<ValidatorDescriptor>>();

        public DefaultValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options)
        {
            ServiceProvider = serviceProvider;

            var validatorResults = AssemblyScanner.FindValidatorsInAssemblies(options.AssembliesToScanForValidators).ToArray();

            if (!validatorResults.Any() && options.ThrowIfNoValidatorsFound)
            {
                throw new Exception($"No validators were found in the provided " +
                    $"{nameof(IFairyBreadOptions)}.{nameof(IFairyBreadOptions.AssembliesToScanForValidators)} " +
                    $"(with concrete type: {options.GetType().FullName}) which included: " +
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
                if (!Cache.TryGetValue(validatedType, out var validatorsForType))
                {
                    Cache[validatedType] = validatorsForType = new List<ValidatorDescriptor>();
                }

                var requiresOwnScope = ShouldBeResolvedInOwnScope(validatorType);

                var validatorDescriptor = new ValidatorDescriptor(validatorType, requiresOwnScope);

                validatorsForType.Add(validatorDescriptor);
            }
        }

        public virtual IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, Argument argument)
        {
            if (Cache.TryGetValue(argument.ClrType, out var validatorDescriptors))
            {
                foreach (var validatorDescriptor in validatorDescriptors)
                {
                    if (validatorDescriptor.RequiresOwnScope)
                    {
                        var scope = ServiceProvider.CreateScope();
                        var validator = (IValidator)scope.ServiceProvider.GetRequiredService(validatorDescriptor.ValidatorType);
                        yield return new ResolvedValidator(validator, scope);
                    }
                    else
                    {
                        var validator = (IValidator)ServiceProvider.GetRequiredService(validatorDescriptor.ValidatorType);
                        yield return new ResolvedValidator(validator);
                    }
                }
            }
        }

        public bool ShouldBeResolvedInOwnScope(Type validatorType)
        {
            return HasOwnScopeInterfaceType.IsAssignableFrom(validatorType);
        }

        /// <summary>
        /// Description of a validator configuration to be stored in the validator descriptor cache.
        /// </summary>
        protected class ValidatorDescriptor
        {
            /// <summary>
            /// Type of the validator.
            /// </summary>
            public Type ValidatorType { get; }

            /// <summary>
            /// Does the validator inherit <see cref="IRequiresOwnScopeValidator"/>? 
            /// If so, this means it should be resolved from the service provider in it's own scope.
            /// </summary>
            public bool RequiresOwnScope { get; }

            /// <summary>
            /// Instantiates a new <see cref="ValidatorDescriptor"/>.
            /// </summary>
            /// <param name="validatorType">The validator.</param>
            public ValidatorDescriptor(Type validatorType, bool requiresOwnScope)
            {
                ValidatorType = validatorType;
                RequiresOwnScope = requiresOwnScope;
            }
        }
    }
}
