using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace FairyBread
{
    internal class ValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationErrorsHandler _validationErrorsHandler;

        public ValidationMiddleware(
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
            var arguments = context.Field.Arguments;

            var invalidResults = new List<ArgumentValidationResult>();

            foreach (var argument in arguments)
            {
                if (argument == null)
                {
                    continue;
                }

                var resolvedValidators = _validatorProvider
                    .GetValidators(context, argument)
                    .ToArray();
                if (resolvedValidators.Length > 0)
                {
                    try
                    {
                        var value = context.ArgumentValue<object?>(argument.Name);
                        if (value == null)
                        {
                            continue;
                        }

                        var isListType = argument.Type.IsListType();
                        Type? valueRuntimeType = null;
                        MethodInfo? toArrayMethod = null;
                        object? arrayValue = null;

                        foreach (var resolvedValidator in resolvedValidators)
                        {
                            // Workaround for https://github.com/ChilliCream/hotchocolate/issues/4350
                            var valueToValidate = value;
                            if (isListType &&
                                !resolvedValidator.Validator.CanValidateInstancesOfType(value.GetType()))
                            {
                                valueRuntimeType ??= value.GetType();
                                toArrayMethod ??= valueRuntimeType.GetMethod("ToArray");
                                if (toArrayMethod != null)
                                {
                                    valueToValidate = (arrayValue ??= toArrayMethod.Invoke(value, null));
                                }
                            }

                            var validationContext = new ValidationContext<object?>(valueToValidate);
                            var validationResult = await resolvedValidator.Validator.ValidateAsync(
                                validationContext,
                                context.RequestAborted);
                            if (validationResult != null &&
                                !validationResult.IsValid)
                            {
                                invalidResults.Add(
                                    new ArgumentValidationResult(
                                        argument.Name,
                                        resolvedValidator.Validator,
                                        validationResult));
                            }
                        }
                    }
                    finally
                    {
                        foreach (var resolvedValidator in resolvedValidators)
                        {
                            resolvedValidator.Scope?.Dispose();
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
    }
}
