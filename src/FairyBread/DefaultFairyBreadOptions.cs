using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace FairyBread
{
    public class DefaultFairyBreadOptions : IFairyBreadOptions
    {
        public virtual bool ThrowIfNoValidatorsFound { get; set; } = true;

        public virtual bool AllowReturnResult { get; set; } = false;

        /// <inheritdoc/>
        public virtual Func<IMiddlewareContext, IInputField, bool> ShouldValidate { get; set; }
            = DefaultImplementations.ShouldValidate;

        /// <summary>
        /// Default implementations of some things that can be re-composed as needed.
        /// </summary>
        public static class DefaultImplementations
        {
            /// <summary>
            /// By default, FairyBread will validate any argument that:
            /// <list type="bullet">
            ///     <item>
            ///         <description>
            ///             is an InputObjectType on a mutation operation,
            ///         </description>
            ///     </item>
            ///     <item>
            ///         <description>
            ///             is manually opted-in at the field level with:
            ///             <list type="bullet">
            ///                 <item><description>
            ///                     [Validate] on the resolver method argument in pure code first
            ///                 </description></item>
            ///                 <item><description>
            ///                     .UseValidation() on the argument definition in code first
            ///                 </description></item>
            ///             </list>
            ///         </description>
            ///     </item>
            ///     <item>
            ///         <description>
            ///             is manually opted-in at the input type level with:
            ///             <list type="bullet">
            ///                 <item><description>
            ///                     [Validate] on the CLR backing type in pure code first
            ///                 </description></item>
            ///                 <item><description>
            ///                     .UseValidation() on the InputObjectType descriptor in code first
            ///                 </description></item>
            ///             </list>
            ///         </description>
            ///     </item>
            ///     <item>
            ///        <description>
            ///            
            ///        </description>
            ///     </item>
            /// </list>
            /// </summary>
            /// <remarks>
            /// More at: https://github.com/benmccallum/fairybread#when-validation-will-fire
            /// </remarks>
            public static bool ShouldValidate(IMiddlewareContext context, IInputField argument)
            {
                // If a mutation operation and this is an input object
                if (context.Operation.Operation == OperationType.Mutation &&
                    argument.Type.InnerType() is InputObjectType)
                {
                    return true;
                }

                if (ShouldValidateBasedOnValidateDescriptor(context, argument))
                {
                    return true;
                }

                return false;
            }

            public static bool ShouldValidateBasedOnValidateDescriptor(
                IMiddlewareContext context,
                IInputField argument)
            {
                // If argument itself was annotated
                if (IsValidateDescriptorApplied(argument.ContextData))
                {
                    return true;
                }

                // If argument's input type or clr type was annotated
                if (argument.Type.InnerType() is InputObjectType inputType &&
                    IsValidateDescriptorApplied(inputType.ContextData))
                {
                    return true;
                }

                return false;
            }

            private static bool IsValidateDescriptorApplied(IReadOnlyDictionary<string, object?> contextData)
            {
                return contextData.TryGetValue(ValidateAttribute.ValidateContextDataKey, out var isValidateDescriptorAppliedRaw) &&
                   isValidateDescriptorAppliedRaw is bool isValidateDescriptorApplied &&
                   isValidateDescriptorApplied;
            }
        }
    }
}
