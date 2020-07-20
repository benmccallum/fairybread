using HotChocolate.Resolvers;
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

        public Func<IMiddlewareContext, bool> ShouldValidate { get; set; } = context => true;
    }
}
