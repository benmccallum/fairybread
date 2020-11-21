using FluentValidation;
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

        /// <summary>
        /// Function used to determine if an argument should be validated by
        /// FairyBread's <see cref="InputValidationMiddleware"/>.
        /// The default implementation is <see cref="DefaultImplementations.ShouldValidate(IMiddlewareContext, Argument)"/>
        /// </summary>
        Func<IMiddlewareContext, Argument, bool> ShouldValidate { get; set; }
    }
}
