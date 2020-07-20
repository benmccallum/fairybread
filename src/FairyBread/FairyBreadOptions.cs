using HotChocolate.Resolvers;
using System;

namespace FairyBread
{

    public class FairyBreadOptions : IFairyBreadOptions
    {
        public bool ThrowIfNoValidatorsFound { get; } = true;

        public Func<IMiddlewareContext, bool> ShouldValidate { get; } = context => true;
    }
}
