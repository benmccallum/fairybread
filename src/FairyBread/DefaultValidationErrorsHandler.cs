namespace FairyBread;

public class DefaultValidationErrorsHandler : IValidationErrorsHandler
{
    public virtual void Handle(
        IMiddlewareContext context,
        IEnumerable<ArgumentValidationResult> invalidResults)
    {
        if (context.Selection.Field.ContextData.TryGetValue(WellKnownContextData.UsesMutationConvention, out var usesMutationConventionObj) &&
            usesMutationConventionObj is bool usesMutationConvention &&
            usesMutationConvention == true)
            // TODO: And options are configured to write this into conventions now
        {
            context.Result = new MutationError(
                invalidResults
                .Select(DefaultValidationError.CreateErrorFrom)
                .ToArray());
            return;
            //// Throw and let the mutation convention's error middleware
            //// pick this up and do what it wants with it.
            //// TODO: Consider if I can short-circuit their middleware
            //// or just give it something they handle rather than doing the whole
            //// exception hoop jump
            //throw new DefaultValidationException(
            //    context,
            //    invalidResults);
        }

        foreach (var invalidResult in invalidResults)
        {
            foreach (var failure in invalidResult.Result.Errors)
            {
                var errorBuilder = CreateErrorBuilder(
                    context,
                    invalidResult.ArgumentName,
                    invalidResult.Validator,
                    failure);
                var error = errorBuilder.Build();
                context.ReportError(error);
            }
        }
    }

    protected virtual IErrorBuilder CreateErrorBuilder(
        IMiddlewareContext context,
        string argumentName,
        IValidator validator,
        ValidationFailure failure)
    {
        var builder = ErrorBuilder.New()
            .SetPath(context.Path)
            .SetMessage(failure.ErrorMessage)
            .SetCode("FairyBread_ValidationError")
            .SetExtension("argumentName", argumentName)
            .SetExtension("validatorName", validator.GetType().Name)
            .SetExtension("errorCode", failure.ErrorCode)
            .SetExtension("errorMessage", failure.ErrorMessage)
            .SetExtension("attemptedValue", failure.AttemptedValue)
            .SetExtension("severity", failure.Severity)
            .SetExtension(
                "formattedMessagePlaceholderValues",
                failure.FormattedMessagePlaceholderValues);

        if (!string.IsNullOrWhiteSpace(failure.PropertyName))
        {
            builder = builder
                .SetExtension("propertyName", failure.PropertyName);
        }

        return builder;
    }
}
