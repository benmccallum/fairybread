using System;
using System.Collections.Generic;
using HotChocolate;

namespace FairyBread
{
    public interface IValidatorRegistry
    {
        Dictionary<Type, List<ValidatorDescriptor>> CacheByArgType { get; }

        Dictionary<FieldCoordinate, List<ValidatorDescriptor>> CacheByFieldCoord { get; }

        bool ShouldBeResolvedInOwnScope(Type validatorType);
    }
}
