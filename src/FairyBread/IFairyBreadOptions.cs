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
        /// If true, FairyBread will only add the validation middleware onto fields
        /// if it finds at least one validator that would be run given the argument's runtime type.
        /// In most cases this should be left on, but it's tunable as an escape hatch if
        /// pruning is causing issues.
        /// </summary>
        bool PruneMiddlewarePlacement { get; }

        /// <summary>
        /// During pruning, if for some reason FairyBread can't determine the argument's
        /// runtime type, if this option is true, FairyBread will throw. This is mostly to
        /// help find gaps in the pruning process. If you encounter an exception, you can turn
        /// this off as FairyBread will prune where it can still and the problem argument will
        /// always get the validation middleware. Or, less ideal, you can turn off pruning entirely
        /// with <see cref="PruneMiddlewarePlacement"/>. In either case, please repor the issue in GitHub.
        /// </summary>
        bool ThrowIfArgumentRuntimeTypeCouldNotBeDeterminedDuringPruning { get; }

        /// <summary>
        /// A function that evaluates an argument during schema building.
        /// If it returns true, the input validation middleware will be added to this field,
        /// else it won't and this field's arguments will not be validated.
        /// The default implementation always returns <c>true</c>.
        /// </summary>
        Func<ObjectTypeDefinition, ObjectFieldDefinition, ArgumentDefinition, bool> ShouldValidateArgument { get; set; }
    }
}
