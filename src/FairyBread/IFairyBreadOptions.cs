using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FairyBread
{
    public interface IFairyBreadOptions
    {
        IEnumerable<Assembly> AssembliesToScanForValidators { get; set; }

        bool ThrowIfNoValidatorsFound { get; set; }

        Func<IMiddlewareContext, Argument, bool> ShouldValidate { get; set; }
    }
}
