using System.Collections.Generic;
using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Resolvers;

namespace FairyBread
{
    public class DefaultValidationErrorsHandler : IValidationErrorsHandler
    {
        public virtual void Handle(
            IMiddlewareContext context,
            IEnumerable<ValidationResult> invalidResults)
        {
            foreach (var invalidResult in invalidResults)
            {
                foreach (var failure in invalidResult.Errors)
                {
                    var error = BuildError(context, failure);
                    context.ReportError(error);
                }
            }
        }

        protected virtual IError BuildError(
            IMiddlewareContext context,
            ValidationFailure failure)
        {
            return ErrorBuilder.New()
                .SetPath(context.Path)
                .SetMessage(failure.ErrorMessage)
                .SetCode("FairyBread_ValidationError")
                .SetExtension("errorCode", failure.ErrorCode)
                .SetExtension("errorMessage", failure.ErrorMessage)
                .SetExtension("propertyName", failure.PropertyName)
                .SetExtension("attemptedValue", failure.AttemptedValue)
                .SetExtension("severity", failure.Severity)
                .SetExtension("formattedMessagePlaceholderValues", failure.FormattedMessagePlaceholderValues)
                .Build();
        }
    }
}
