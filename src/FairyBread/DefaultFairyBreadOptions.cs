using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FairyBread
{
    public class DefaultFairyBreadOptions : IFairyBreadOptions
    {
        public virtual IEnumerable<Assembly> AssembliesToScanForValidators { get; set; } = Enumerable.Empty<Assembly>();

        public virtual bool ThrowIfNoValidatorsFound { get; set; } = true;

        public virtual Func<IMiddlewareContext, Argument, bool> ShouldValidate { get; set; } 
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
