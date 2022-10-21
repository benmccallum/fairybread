namespace FairyBread;

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
        var arguments = context.Selection.Field.Arguments;

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

                    foreach (var resolvedValidator in resolvedValidators)
                    {
                        var validationContext = new ValidationContext<object?>(value);
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