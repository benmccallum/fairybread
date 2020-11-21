using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;

namespace FairyBread
{
    public class ValidationErrorFilter : IErrorFilter
    {
        /// <summary>
        /// Function used to determine if an <see cref="IError"/> with a <see cref="ValidationException"/>
        /// should be handled and rewritten.
        /// </summary>
        private Func<ValidationException, bool> _shouldHandleValidationExceptionPredicate { get; set; } = ex => true;

        public ValidationErrorFilter()
        {

        }

        public ValidationErrorFilter(Func<ValidationException, bool> shouldHandleValidationExceptionPredicate)
        {
            _shouldHandleValidationExceptionPredicate = shouldHandleValidationExceptionPredicate;
        }

        public IError OnError(IError error)
        {
            if (error.Exception != null &&
                error.Exception is ValidationException validationException &&
                _shouldHandleValidationExceptionPredicate(validationException))
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
