using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Resolvers;

namespace FairyBread
{
    public class DefaultValidationResultHandler : IValidationResultHandler
    {
        public virtual bool Handle(IMiddlewareContext context, ValidationResult result)
        {
            if (result.IsValid)
            {
                HandleValid(context, result);
                return true;
            }
            else
            {
                HandleInvalid(context, result);
                return false;
            }
        }

        protected virtual void HandleInvalid(IMiddlewareContext context, ValidationResult result)
        {
            foreach (var failure in result.Errors)
            {
                HandleFailure(context, failure);
            }
        }

        protected virtual void HandleFailure(IMiddlewareContext context, ValidationFailure failure)
        {
            var error = ExtractError(context, failure)
                .Build();

            context.ReportError(error);
        }

        protected virtual IErrorBuilder ExtractError(IMiddlewareContext context, ValidationFailure failure)
            => ErrorBuilder.New()
                .SetMessage(failure.ErrorMessage)
                .SetExtension(nameof(failure.ResourceName), failure.ResourceName)
                .SetExtension(nameof(failure.PropertyName), failure.PropertyName)
                .SetExtension(nameof(failure.ErrorCode), failure.ErrorCode)
                .SetExtension(nameof(failure.Severity), failure.Severity)
                .AddLocation(context.FieldSelection)
                .SetPath(context.Path);

        protected virtual void HandleValid(IMiddlewareContext context, ValidationResult result)
        {

        }
    }
}
