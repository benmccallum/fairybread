using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FairyBread
{
    public class FairyBreadOptions : IFairyBreadOptions
    {
        public IEnumerable<Assembly> AssembliesToScanForValidators { get; set; } = Enumerable.Empty<Assembly>();

        public bool ThrowIfNoValidatorsFound { get; set; } = true;

        public Func<IMiddlewareContext, Argument, bool> ShouldValidate { get; set; } 
            = (context, argument) =>
            {
                if (context.Operation.Operation == OperationType.Mutation)
                {
                    return true;
                }

                // TODO: If argument has attribute on it
                // ...

                return false;
            };
    }
}
