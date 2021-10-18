using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace FairyBread
{
    public interface IFairyBreadOptions
    {
        /// <summary>
        /// If true, FairyBread will throw on startup if no validators are found.
        /// This is <c>true</c> by default to avoid an accidental release with no validators
        /// that continues to function silently but would obviously be very dangerous.
        /// </summary>
        bool ThrowIfNoValidatorsFound { get; set; }

        /// <summary>
        /// If true, FairyBread will only add the validation middleware onto fields with an
        /// argument whose runtime type can be determined and whose runtime type has a validator.
        /// In most cases this should be left on, but it's tunable as an escape hatch if it's causing issues.
        /// </summary>
        bool OptimizeMiddlewarePlacement { get; }

        /// <summary>
        /// A function that evaluates an argument during schema building.
        /// If it returns true, validation will occur on this argument at runtime, else it won't.
        /// The default implementation always returns <c>true</c>.
        /// </summary>
        Func<ObjectTypeDefinition, ObjectFieldDefinition, ArgumentDefinition, bool> ShouldValidateArgument { get; set; }
    }
}
