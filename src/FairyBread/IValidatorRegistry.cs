using System;
using System.Collections.Generic;

namespace FairyBread
{
    /// <summary>
    /// Maintains a registry of validators.
    /// </summary>
    public interface IValidatorRegistry
    {
        Dictionary<Type, List<ValidatorDescriptor>> Cache { get; }

        bool ShouldBeResolvedInOwnScope(Type validatorType);
    }
}
