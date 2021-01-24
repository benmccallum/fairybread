using System.Collections.Generic;
using FluentValidation.Results;
using HotChocolate.Resolvers;

namespace FairyBread
{
    public interface IValidationErrorsHandler
    {
        void Handle(
            IMiddlewareContext context,
            IEnumerable<ValidationResult> invalidResults);
    }
}
