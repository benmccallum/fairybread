using HotChocolate.Resolvers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FairyBread
{
    public interface IFairyBreadOptions
    {
        public IEnumerable<Assembly> AssembliesToScanForValidators { get; set; }

        public bool ThrowIfNoValidatorsFound { get; set; }

        Func<IMiddlewareContext, bool> ShouldValidate { get; set; }
    }
}
