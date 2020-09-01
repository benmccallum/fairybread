﻿using FluentValidation;
using FluentValidation.Results;
using HotChocolate.Resolvers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FairyBread
{
    public class InputValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IFairyBreadOptions _options;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationResultHandler _validationResultHandler;

        public InputValidationMiddleware(FieldDelegate next,
            IFairyBreadOptions options,
            IValidatorProvider validatorProvider,
            IValidationResultHandler validationResultHandler)
        {
            _next = next;
            _options = options;
            _validatorProvider = validatorProvider;
            _validationResultHandler = validationResultHandler;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;

            var validationResults = new List<ValidationResult>();

            foreach (var argument in arguments)
            {
                if (argument == null ||
                    !_options.ShouldValidate(context, argument))
                {
                    continue;
                }

                var resolvedValidators = _validatorProvider.GetValidators(context, argument);
                try
                {
                    var value = context.Argument<object>(argument.Name);
                    foreach (var resolvedValidator in resolvedValidators)
                    {
                        var validationContext = new ValidationContext<object>(value);
                        var validationResult = await resolvedValidator.Validator.ValidateAsync(validationContext, context.RequestAborted);
                        if (validationResult != null)
                        {
                            validationResults.Add(validationResult);
                            _validationResultHandler.Handle(context, validationResult);
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

            var invalidValidationResults = validationResults.Where(r => !r.IsValid);
            if (invalidValidationResults.Any())
            {
                OnInvalid(context, invalidValidationResults);
            }

            await _next(context);
        }

        protected virtual void OnInvalid(IMiddlewareContext context, IEnumerable<ValidationResult> invalidValidationResults)
        {
            throw new ValidationException(invalidValidationResults.SelectMany(vr => vr.Errors));
        }
    }
}
