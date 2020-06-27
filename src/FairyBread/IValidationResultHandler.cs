using FluentValidation.Results;
using HotChocolate.Resolvers;

namespace FairyBread
{
    public interface IValidationResultHandler
    {
        void Handle(IMiddlewareContext context, ValidationResult result);
    }
}
