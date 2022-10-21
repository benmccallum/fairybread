using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace FairyBread;

/// <summary>
/// Resolves validators at query execution time.
/// </summary>
public interface IValidatorProvider
{
    IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument);
}