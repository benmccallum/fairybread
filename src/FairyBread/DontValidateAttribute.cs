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
    public class DontValidateAttribute : ArgumentDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
            {
                argDef.ContextData[WellKnownContextData.DontValidate] = true;
            });
        }
    }
}
