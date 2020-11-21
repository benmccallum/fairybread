using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Generic;

namespace FairyBread
{
    public interface IValidatorProvider
    {
        IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument);

        bool ShouldBeResolvedInOwnScope(Type validatorType);
    }
}
