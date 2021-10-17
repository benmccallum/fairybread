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
        /// In most cases this should be left on, but it's tunable as an escape hatch if it's causing issues.
        /// </summary>
        bool OptimizeMiddlewarePlacement { get; }

        /// <summary>
        /// During middleware placement, if for some reason FairyBread can't determine the argument's
        /// runtime type, if this option is true, FairyBread will throw. This is mostly to
        /// help find gaps in the optimization process. If you encounter an exception, you can turn
        /// this off as FairyBread will optimize where it can still and the problem argument will just
        /// always get the validation middleware. Or, less ideal, you can turn off optimization entirely
        /// with <see cref="OptimizeMiddlewarePlacement"/>. In either case, please repor the issue in GitHub.
        /// </summary>
        bool ThrowIfArgumentRuntimeTypeCouldNotBeDeterminedWhileOptimizingMiddlewarePlacement { get; }

        /// <summary>
        /// A function that evaluates an argument during schema building.
        /// If it returns true, validation will occur on this argument at runtime, else it won't.
        /// The default implementation always returns <c>true</c>.
        /// </summary>
        Func<ObjectTypeDefinition, ObjectFieldDefinition, ArgumentDefinition, bool> ShouldValidateArgument { get; set; }
    }
}
