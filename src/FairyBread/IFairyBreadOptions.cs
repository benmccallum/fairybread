using System;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace FairyBread
{
    public interface IFairyBreadOptions
    {
        /// <summary>
        /// If true, FairyBread will throw on startup if no validators are found.
        /// This is on by default to avoid an accidental release with no validators
        /// that continues to function silently but would obviously be very dangerous.
        /// </summary>
        bool ThrowIfNoValidatorsFound { get; set; }

        /// <summary>
        /// If true, FairyBread will set the current field's <c>IResolverContext.Result</c> 
        /// to <c>null</c> when a validation error occurs.
        /// </summary>
        bool SetNullResultOnValidationError { get; set; }

        /// <summary>
        /// Function used to determine if an argument should be validated by
        /// FairyBread's <see cref="InputValidationMiddleware"/>.
        /// The default implementation is
        /// <see cref="DefaultImplementations.ShouldValidate(IMiddlewareContext, Argument)"/>
        /// </summary>
        Func<IMiddlewareContext, IInputField, bool> ShouldValidate { get; set; }
    }
}
