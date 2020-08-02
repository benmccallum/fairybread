using FluentValidation.Results;
using HotChocolate.Resolvers;

namespace FairyBread
{
    /// <summary>
    /// Handles a <see cref="ValidationResult"/>.
    /// </summary>
    public interface IValidationResultHandler
    {
        /// <summary>
        /// Handles a <see cref="ValidationResult"/>, returning true if everything is OK.
        /// </summary>
        /// <param name="context">The current middleware context.</param>
        /// <param name="result">The result to inspect.</param>
        /// <returns>Returns true if everything is OK, or false is validation failures were present.</returns>
        bool Handle(IMiddlewareContext context, ValidationResult result);
    }
}
