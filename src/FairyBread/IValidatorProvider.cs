using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace FairyBread
{
    public interface IValidatorProvider
    {
        IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument);
    }
}
