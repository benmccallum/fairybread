using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Generic;

namespace FairyBread
{
    public interface IValidatorProvider
    {
        IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, Argument argument);

        bool ShouldBeResolvedInOwnScope(Type validatorType);
    }
}
