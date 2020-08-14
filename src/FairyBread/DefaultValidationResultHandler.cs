using FluentValidation.Results;
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

        }

        protected virtual void HandleValid(IMiddlewareContext context, ValidationResult result)
        {

        }
    }
}
