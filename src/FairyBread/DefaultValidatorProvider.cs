using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace FairyBread
{
    public class DefaultValidatorProvider : IValidatorProvider
    {
        protected readonly IServiceProvider ServiceProvider;
        protected static readonly Type HasOwnScopeInterfaceType = typeof(IRequiresOwnScopeValidator);
        protected readonly Dictionary<Type, List<ValidatorDescriptor>> Cache = new Dictionary<Type, List<ValidatorDescriptor>>();

        public DefaultValidatorProvider(IServiceProvider serviceProvider, IServiceCollection services, IFairyBreadOptions options)
        {
            ServiceProvider = serviceProvider;

            var validatorResults = services
                .Where(s => typeof(IValidator).IsAssignableFrom(s.ImplementationType))
                .Select()
                .Select(s => (s.ImplementationType, s.ImplementationType.GetGenericArguments()[0]))
                .ToArray();

            //List<Type> genTypes = new List<Type>();
            //foreach (Type intType in t.GetInterfaces())
            //{
            //    if (intType.IsGenericType && intType.GetGenericTypeDefinition()
            //        == typeof(IGeneric<>))
            //    {
            //        genTypes.Add(intType.GetGenericArguments()[0]);
            //    }
            //}

            //if (!validatorResults.Any() && options.ThrowIfNoValidatorsFound)
            //{
            //    throw new Exception($"No validators were found in the provided " +
            //        $"{nameof(IFairyBreadOptions)}.{nameof(IFairyBreadOptions.AssembliesToScanForValidators)} " +
            //        $"(with concrete type: {options.GetType().FullName}) which included: " +
            //        $"{string.Join(",", options.AssembliesToScanForValidators)}.");
            //}

            //foreach (var validatorResult in validatorResults)
            //{
            //    var validatorType = validatorResult.ValidatorType;
            //    if (validatorType.IsAbstract)
            //    {
            //        continue;
            //    }

            //    var validatedType = validatorResult.InterfaceType.GenericTypeArguments.Single();
            //    if (!Cache.TryGetValue(validatedType, out var validatorsForType))
            //    {
            //        Cache[validatedType] = validatorsForType = new List<ValidatorDescriptor>();
            //    }

            //    var requiresOwnScope = ShouldBeResolvedInOwnScope(validatorType);

            //    var validatorDescriptor = new ValidatorDescriptor(validatorType, requiresOwnScope);

            //    validatorsForType.Add(validatorDescriptor);
            //}
        }

        public virtual IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
        {
            if (Cache.TryGetValue(argument.RuntimeType, out var validatorDescriptors))
            {
                foreach (var validatorDescriptor in validatorDescriptors)
                {
                    if (validatorDescriptor.RequiresOwnScope)
                    {
                        var scope = ServiceProvider.CreateScope(); // resolved by middleware
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
