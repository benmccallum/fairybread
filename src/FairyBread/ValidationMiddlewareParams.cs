using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace FairyBread
{
    internal class ValidationMiddlewareParams
    {
        private readonly FieldDelegate _next;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationErrorsHandler _validationErrorsHandler;

        public ValidationMiddlewareParams(
            FieldDelegate next,
            IValidatorProvider validatorProvider,
            IValidationErrorsHandler validationErrorsHandler)
        {
            _next = next;
            _validatorProvider = validatorProvider;
            _validationErrorsHandler = validationErrorsHandler;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            context.ContextData[WellKnownContextData.ValidatorDescriptorsParams] = true;
            var arguments = context.Selection.Field.Arguments;

            var invalidResults = new List<ArgumentValidationResult>();
            var type = context.Selection.Field.ResolverMember as MethodInfo;
            var parameters = type.GetParameters();



            foreach (var argument in context.Selection.Field.Arguments)
            {
                if (argument == null)
                {
                    continue;
                }

                var resolvedValidators = GetValidators(context, argument).GroupBy(x => x.param);
                if (resolvedValidators.Count() > 0)
                {
                    foreach (var resolvedValidatorGroup in resolvedValidators)
                    {
                        try
                        {
                            var value = context.ArgumentValue<object?>(resolvedValidatorGroup.Key);
                            if (value == null)
                            {
                                continue;
                            }

                            foreach (var resolvedValidator in resolvedValidatorGroup)
                            {
                                var validationContext = new ValidationContext<object?>(value);
                                var validationResult = await resolvedValidator.resolver.Validator.ValidateAsync(
                                    validationContext,
                                    context.RequestAborted);
                                if (validationResult != null &&
                                    !validationResult.IsValid)
                                {
                                    invalidResults.Add(
                                        new ArgumentValidationResult(
                                            resolvedValidatorGroup.Key,
                                            resolvedValidator.resolver.Validator,
                                            validationResult));
                                }
                            }
                        }
                        finally
                        {
                            foreach (var resolvedValidator in resolvedValidatorGroup)
                            {
                                resolvedValidator.resolver.Scope?.Dispose();
                            }
                        }
                    }
                }
            }

            if (invalidResults.Any())
            {
                _validationErrorsHandler.Handle(context, invalidResults);
                return;
            }

            await _next(context);
        }

        IEnumerable<(string param, ResolvedValidator resolver)> GetValidators(IMiddlewareContext context, IInputField argument)
        {
            if (!argument.ContextData.TryGetValue(WellKnownContextData.ValidatorDescriptorsParams, out var validatorDescriptorsRaw)
                || validatorDescriptorsRaw is not Dictionary<string, List<ValidatorDescriptor>> validatorDescriptors)
            {
                yield break;
            }

            foreach (var validatorDescriptor in validatorDescriptors)
            {
                foreach (var validatorDescriptorEntry in validatorDescriptor.Value)
                {
                    if (validatorDescriptorEntry.RequiresOwnScope)
                    {
                        var scope = context.Services.CreateScope(); // disposed by middleware
                        var validator = (IValidator)scope.ServiceProvider.GetRequiredService(validatorDescriptorEntry.ValidatorType);
                        yield return (validatorDescriptor.Key, new ResolvedValidator(validator, scope));
                    }
                    else
                    {
                        var validator = (IValidator)context.Services.GetRequiredService(validatorDescriptorEntry.ValidatorType);
                        yield return (validatorDescriptor.Key, new ResolvedValidator(validator));
                    }
                }
            }
        }
    }
}
