using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace FairyBread
{
    /// <summary>
    /// Instructs FairyBread to not run any validators that
    /// are implicitly associated with the annotated argument's type.
    /// Explicit validators will still be run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DisableImplicitValidationAttribute : ArgumentDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            descriptor.DisableImplicitValidation();
        }
    }

    public static class DisableImplicitValidationArgumentDescriptorExtensions
    {
        /// <summary>
        /// Instructs FairyBread to not run any validators that
        /// are implicitly associated with this argument's runtime type.
        /// Explicit validators will still be run.
        /// </summary>
        public static IArgumentDescriptor DisableImplicitValidation(
            this IArgumentDescriptor descriptor)
        {
            descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
            {
                argDef.ContextData[WellKnownContextData.DisableImplicitValidation] = true;
            });

            return descriptor;
        }
    }

    /// <summary>
    /// Instructs FairyBread to not run any validators that
    /// are implicitly associated with the annotated argument's type.
    /// Explicit validators will still be run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    [Obsolete("Use DisableImplicitValidationAttribute")]
    public class DontValidateImplicitlyAttribute : ArgumentDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            descriptor.DontValidateImplicitly();
        }
    }

    [Obsolete("Use DisableImplicitValidationArgumentDescriptorExtensions")]
    public static class DontValidateImplicitlyArgumentDescriptorExtensions
    {
        /// <summary>
        /// Instructs FairyBread to not run any validators that
        /// are implicitly associated with this argument's runtime type.
        /// Explicit validators will still be run.
        /// </summary>
        [Obsolete("Use DisableImplicitValidation")]
        public static IArgumentDescriptor DontValidateImplicitly(
            this IArgumentDescriptor descriptor)
        {
            descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
            {
                argDef.ContextData[WellKnownContextData.DisableImplicitValidation] = true;
            });

            return descriptor;
        }
    }
}
