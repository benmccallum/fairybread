using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;

namespace FairyBread
{
    public class DefaultValidationErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            if (error.Exception != null &&
                error.Exception is ValidationException validationException)
            {
                return OnValidationException(error, validationException);
            }

            return error;
        }

        protected virtual IError OnValidationException(IError error, ValidationException validationException)
        {
            return error
                .WithMessage("Validation errors occurred.")
                .WithCode("FairyBread_ValidationError")
                .AddExtension("Failures", validationException.Errors.Select(FormatFailure))
                .RemoveException();
        }

        protected virtual object FormatFailure(ValidationFailure failure)
        {
            return new
            {
                failure.ErrorCode,
                failure.ErrorMessage,
                failure.PropertyName,
                failure.ResourceName,
                failure.AttemptedValue,
                failure.Severity,
                failure.FormattedMessageArguments,
                failure.FormattedMessagePlaceholderValues
            };
        }
    }
}
