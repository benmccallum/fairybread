using HotChocolate.Resolvers;
using System;

namespace FairyBread
{
    public interface IFairyBreadOptions
    {
        public bool ThrowIfNoValidatorsFound { get; }

        Func<IMiddlewareContext, bool> ShouldValidate { get; }
    }
}
