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
        protected readonly IValidatorRegistry ValidatorRegistry;

        public DefaultValidatorProvider(
            IValidatorRegistry validatorRegistry)
        {
            ValidatorRegistry = validatorRegistry;
        }

        public virtual IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
        {
            var validators = new List<ResolvedValidator>();

            // Explicit validate attribute
            ValidateAttribute? validateAttr = null;
            if (argument.ContextData.TryGetValue(WellKnownContextData.ValidateAttribute, out var rawAttr) &&
                rawAttr is ValidateAttribute valAttr)
            {
                validateAttr = valAttr;
                var isInited = ValidatorRegistry.CacheByFieldCoord.ContainsKey(argument.Coordinate);
                if (!isInited)
                {
                    var validatorDescriptors =
                        ValidatorRegistry.CacheByFieldCoord[argument.Coordinate] =
                        new List<ValidatorDescriptor>();

                    // Unravel them into the cache
                    foreach (var validatorType in validateAttr.ValidatorTypes)
                    {
                        // TODO: v8.0.0, if validator has already been resolved in implicit validators, don't add it again
                        // (In case someone has added an explicit one without realising it'd already be picked up implicitly)

                        var requiresOwnScope = ValidatorRegistry.ShouldBeResolvedInOwnScope(validatorType);
                        validatorDescriptors.Add(new ValidatorDescriptor(validatorType, requiresOwnScope));
                    }
                }
            }

            // Implicit validator/s
            if (validateAttr is null || validateAttr.RunImplicitValidators)
            {
                if (ValidatorRegistry.CacheByArgType.TryGetValue(argument.RuntimeType, out var validatorDescriptors) &&
                    validatorDescriptors is not null)
                {
                    ProcessDescriptors(context, validators, validatorDescriptors);
                }
            }

            // Explicit validator/s
            if (validateAttr is not null)
            {
                var validatorDescriptors = ValidatorRegistry.CacheByFieldCoord[argument.Coordinate];
                ProcessDescriptors(context, validators, validatorDescriptors);
            }

            return validators;
        }

        private static void ProcessDescriptors(
            IMiddlewareContext context,
            List<ResolvedValidator> validators,
            List<ValidatorDescriptor> validatorDescriptors)
        {
            foreach (var validatorDescriptor in validatorDescriptors)
            {
                if (validatorDescriptor.RequiresOwnScope)
                {
                    var scope = context.Services.CreateScope(); // disposed by middleware
                    var validator = (IValidator)scope.ServiceProvider.GetRequiredService(validatorDescriptor.ValidatorType);
                    validators.Add(new ResolvedValidator(validator, scope));
                }
                else
                {
                    var validator = (IValidator)context.Services.GetRequiredService(validatorDescriptor.ValidatorType);
                    validators.Add(new ResolvedValidator(validator));
                }
            }
        }
    }
}
