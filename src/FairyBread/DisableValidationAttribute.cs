using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace FairyBread
{
    /// <summary>
    /// Instructs FairyBread to not run any validation on the annotated argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DisableValidationAttribute : ArgumentDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            descriptor.DisableValidation();
        }
    }

    public static class DisableValidationArgumentDescriptorExtensions
    {
        /// <summary>
        /// Instructs FairyBread to not run any validation for this argument.
        /// </summary>
        public static IArgumentDescriptor DisableValidation(
            this IArgumentDescriptor descriptor)
        {
            descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
            {
                argDef.ContextData[WellKnownContextData.DisableValidation] = true;
            });

            return descriptor;
        }
    }

    /// <summary>
    /// Instructs FairyBread to not run any validation on the annotated argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    [Obsolete("Use DisableValidationAttribute")]
    public class DontValidateAttribute : ArgumentDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            descriptor.DontValidate();
        }
    }

    [Obsolete("Use DisableValidationArgumentDescriptorExtensions")]
    public static class DontValidateArgumentDescriptorExtensions
    {
        /// <summary>
        /// Instructs FairyBread to not run any validation for this argument.
        /// </summary>
        [Obsolete("Use DisableValidation")]
        public static IArgumentDescriptor DontValidate(
            this IArgumentDescriptor descriptor)
        {
            descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
            {
                argDef.ContextData[WellKnownContextData.DisableValidation] = true;
            });

            return descriptor;
        }
    }
}
