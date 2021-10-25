using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace FairyBread
{
    public interface IValidationErrorsHandler
    {
        void Handle(
            IMiddlewareContext context,
            IEnumerable<ArgumentValidationResult> invalidResults);
    }
}
