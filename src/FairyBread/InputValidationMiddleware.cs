using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate.Resolvers;

namespace FairyBread
{
    public class InputValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IFairyBreadOptions _options;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationErrorsHandler _validationErrorsHandler;

        public InputValidationMiddleware(FieldDelegate next,
            IFairyBreadOptions options,
            IValidatorProvider validatorProvider,
            IValidationErrorsHandler validationErrorsHandler)
        {
            _next = next;
            _options = options;
            _validatorProvider = validatorProvider;
            _validationErrorsHandler = validationErrorsHandler;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;

            var invalidResults = new List<ValidationResult>();

            foreach (var argument in arguments)
            {
                if (argument == null ||
                    !_options.ShouldValidate(context, argument))
                {
                    continue;
                }

                var resolvedValidators = _validatorProvider
                    .GetValidators(context, argument)
                    .ToArray();

                try
                {
                    var value = context.ArgumentValue<object?>(argument.Name);
                    if (value == null)
                    {
                        continue;
                    }

                    foreach (var resolvedValidator in resolvedValidators)
                    {
                        var validationContext = new ValidationContext<object?>(value);
                        var validationResult = await resolvedValidator.Validator.ValidateAsync(
                            validationContext,
                            context.RequestAborted);
                        if (validationResult != null &&
                            !validationResult.IsValid)
                        {
                            invalidResults.Add(validationResult);
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

            if (invalidResults.Any())
            {
                _validationErrorsHandler.Handle(context, invalidResults);

                if(!_options.AllowReturnResult)
                {
                    context.Result = null;
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
