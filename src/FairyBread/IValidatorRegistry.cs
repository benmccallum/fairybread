using System;
using System.Collections.Generic;

namespace FairyBread
{
    public interface IValidatorRegistry
    {
        Dictionary<Type, List<ValidatorDescriptor>> Cache { get; }

        bool ShouldBeResolvedInOwnScope(Type validatorType);
    }
}
