using FluentValidation;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Default implementation doesn't require them")]
        private IError OnValidationException(IError error, ValidationException validationException)
        {
            return error
                .WithMessage("Validation errors occurred.")
                .RemoveException();
        }
    }
}
