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

            var validatorDescriptors = (IReadOnlyList<ValidatorDescriptor>)argument.ContextData[WellKnownContextData.ValidatorDescriptors];
            ProcessDescriptors(context, validators, validatorDescriptors);

            return validators;
        }

        private static void ProcessDescriptors(
            IMiddlewareContext context,
            List<ResolvedValidator> validators,
            IReadOnlyList<ValidatorDescriptor> validatorDescriptors)
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
