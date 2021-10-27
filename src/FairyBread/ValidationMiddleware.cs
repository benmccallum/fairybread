using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Newtonsoft.Json;

namespace FairyBread
{
    internal class ValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationErrorsHandler _validationErrorsHandler;
        private static readonly Type _listGenericTypeDef = typeof(List<>);

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
                        Type? eleType = null;
                        Array? array = null;
                        object arrayValue = null;

                        foreach (var resolvedValidator in resolvedValidators)
                        {
                            object valueToValidate = value;
                            if (isListType &&
                                !resolvedValidator.Validator.CanValidateInstancesOfType(value.GetType()))
                            {
                                eleType ??= argument.Type.ElementType().ToRuntimeType();
                                array ??= Array.CreateInstance(eleType, 0);
                                var json = JsonConvert.SerializeObject(value);
                                valueToValidate = arrayValue ??= JsonConvert.DeserializeObject(json, array.GetType());
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
