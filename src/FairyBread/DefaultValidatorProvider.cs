using System;
using System.Collections.Generic;
using FluentValidation;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace FairyBread
{
    public class DefaultValidatorProvider : IValidatorProvider
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IValidatorRegistry ValidatorRegistry;

        public DefaultValidatorProvider(
            IServiceProvider serviceProvider,
            IValidatorRegistry validatorRegistry)
        {
            ServiceProvider = serviceProvider;
            ValidatorRegistry = validatorRegistry;
        }

        public virtual IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
        {
            if (ValidatorRegistry.Cache.TryGetValue(argument.RuntimeType, out var validatorDescriptors))
            {
                foreach (var validatorDescriptor in validatorDescriptors)
                {
                    if (validatorDescriptor.RequiresOwnScope)
                    {
                        var scope = ServiceProvider.CreateScope(); // disposed by middleware
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
    }
}
