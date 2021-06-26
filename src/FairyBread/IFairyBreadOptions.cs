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
        /// A function that evaluates an argument during schema building.
        /// If it returns true, the input validation middleware will be added to this field,
        /// else it won't and this field's arguments will not be validated.
        /// The default implementation always returns <c>true</c>.
        /// </summary>
        Func<ObjectTypeDefinition, ObjectFieldDefinition, ArgumentDefinition, bool> ShouldValidateArgument { get; set; }
    }
}
